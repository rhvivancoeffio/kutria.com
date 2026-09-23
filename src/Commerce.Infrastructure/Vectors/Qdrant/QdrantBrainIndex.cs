using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;
using OpenAI.Embeddings;
using Qdrant.Client;
using Qdrant.Client.Grpc;
using Commerce.Application.Abstracts;
using Commerce.Infrastructure.Vectors.Common;
using static Qdrant.Client.Grpc.Conditions;

namespace Commerce.Infrastructure.Vectors.Qdrant;

public sealed class QdrantBrainIndex : IBrainIndex
{
    private readonly QdrantClient _client;
    private readonly EmbeddingClient _embeddings;
    private readonly ILogger<QdrantBrainIndex> _logger;
    private readonly ConcurrentDictionary<string, ulong> _sizes = new(StringComparer.Ordinal);
    private readonly SemaphoreSlim _ready = new(1, 1);

    public QdrantBrainIndex(
        string host,
        int port,
        bool https,
        string endpoint,
        string apiKey,
        string deployment,
        ILogger<QdrantBrainIndex> logger)
    {
        _client = new QdrantClient(host, port, https);
        _embeddings = AzureOpenAiCompatibleClient.Create(endpoint, apiKey)
            .GetEmbeddingClient(deployment);
        _logger = logger;
    }

    public async Task<IReadOnlyList<BrainHit>> SearchAsync(BrainSearch search, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(search.CollectionName)
            || string.IsNullOrWhiteSpace(search.TenantId)
            || string.IsNullOrWhiteSpace(search.Text)
            || search.Limit <= 0)
        {
            return [];
        }

        if (!await _client.CollectionExistsAsync(search.CollectionName, cancellationToken))
        {
            return [];
        }

        _logger.LogInformation(
            "Qdrant brain vector search. Collection={Collection} TenantId={TenantId} Limit={Limit} Query={Query}",
            search.CollectionName,
            search.TenantId,
            search.Limit,
            Truncate(search.Text));

        var embedding = await _embeddings.GenerateEmbeddingAsync(search.Text, cancellationToken: cancellationToken);
        var vector = embedding.Value.ToFloats().ToArray();
        var filter = new Filter { Must = { MatchKeyword("tenant_id", search.TenantId) } };
        if (search.MustMatch is not null)
        {
            foreach (var pair in search.MustMatch)
            {
                if (!string.IsNullOrWhiteSpace(pair.Value))
                {
                    filter.Must.Add(MatchKeyword(pair.Key, pair.Value));
                }
            }
        }

        var results = await _client.SearchAsync(
            search.CollectionName,
            vector,
            filter: filter,
            limit: (ulong)search.Limit,
            cancellationToken: cancellationToken);

        var hits = results
            .Select(hit => new BrainHit(hit.Score, hit.Payload.ToDictionary(pair => pair.Key, pair => Read(pair.Value))))
            .ToList();

        _logger.LogInformation(
            "Qdrant brain vector search done. Collection={Collection} TenantId={TenantId} QueryDims={Dims} Hits={Hits}",
            search.CollectionName,
            search.TenantId,
            vector.Length,
            hits.Count);

        return hits;
    }

    public async Task UpsertAsync(
        string collectionName,
        IReadOnlyList<BrainPoint> points,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(collectionName) || points.Count == 0)
        {
            throw new ArgumentException("A collection and at least one point are required.");
        }

        _logger.LogInformation(
            "Qdrant brain upsert. Collection={Collection} Points={Points}",
            collectionName,
            points.Count);

        var first = await _embeddings.GenerateEmbeddingAsync(points[0].TextToEmbed, cancellationToken: cancellationToken);
        var firstVector = first.Value.ToFloats().ToArray();
        await EnsureCollectionAsync(collectionName, (ulong)firstVector.Length, cancellationToken);
        var structs = new List<PointStruct> { ToPoint(points[0], firstVector) };
        LogBrainPoint(collectionName, points[0], firstVector.Length);

        for (var i = 1; i < points.Count; i++)
        {
            var embedding = await _embeddings.GenerateEmbeddingAsync(points[i].TextToEmbed, cancellationToken: cancellationToken);
            var vector = embedding.Value.ToFloats().ToArray();
            structs.Add(ToPoint(points[i], vector));
            LogBrainPoint(collectionName, points[i], vector.Length);
        }

        await _client.UpsertAsync(collectionName, structs, cancellationToken: cancellationToken);
        _logger.LogInformation(
            "Qdrant brain upsert done. Collection={Collection} Points={Points}",
            collectionName,
            structs.Count);
    }

    private void LogBrainPoint(string collectionName, BrainPoint point, int dims)
    {
        _logger.LogInformation(
            "Qdrant brain upsert point. Collection={Collection} PointId={PointId} VectorDims={Dims} TextToEmbed={Text} PayloadKeys={PayloadKeys}",
            collectionName,
            point.PointId,
            dims,
            Truncate(point.TextToEmbed),
            string.Join(',', point.Payload.Keys));
    }

    public async Task SetPayloadAsync(
        string collectionName,
        IReadOnlyDictionary<string, string> match,
        IReadOnlyDictionary<string, string> payload,
        CancellationToken cancellationToken = default)
    {
        if (!await _client.CollectionExistsAsync(collectionName, cancellationToken))
        {
            return;
        }

        var values = payload.ToDictionary(
            pair => pair.Key,
            pair => new Value { StringValue = pair.Value ?? string.Empty });
        await _client.SetPayloadAsync(collectionName, values, FilterOf(match, null), cancellationToken: cancellationToken);
    }

    public async Task DeleteMatchingAsync(
        string collectionName,
        IReadOnlyDictionary<string, string> match,
        IReadOnlyDictionary<string, string>? except,
        CancellationToken cancellationToken = default)
    {
        if (!await _client.CollectionExistsAsync(collectionName, cancellationToken))
        {
            return;
        }

        await _client.DeleteAsync(collectionName, FilterOf(match, except), cancellationToken: cancellationToken);
    }

    private static Filter FilterOf(IReadOnlyDictionary<string, string> match, IReadOnlyDictionary<string, string>? except)
    {
        var filter = new Filter();
        foreach (var pair in match)
        {
            if (!string.IsNullOrWhiteSpace(pair.Value))
            {
                filter.Must.Add(MatchKeyword(pair.Key, pair.Value));
            }
        }

        if (except is not null)
        {
            foreach (var pair in except)
            {
                if (!string.IsNullOrWhiteSpace(pair.Value))
                {
                    filter.MustNot.Add(MatchKeyword(pair.Key, pair.Value));
                }
            }
        }

        return filter;
    }

    private async Task EnsureCollectionAsync(string collectionName, ulong size, CancellationToken cancellationToken)
    {
        if (_sizes.TryGetValue(collectionName, out var known) && known == size)
        {
            return;
        }

        await _ready.WaitAsync(cancellationToken);
        try
        {
            if (!await _client.CollectionExistsAsync(collectionName, cancellationToken))
            {
                await _client.CreateCollectionAsync(
                    collectionName,
                    new VectorParams { Size = size, Distance = Distance.Cosine },
                    cancellationToken: cancellationToken);
            }

            _sizes[collectionName] = size;
        }
        finally
        {
            _ready.Release();
        }
    }

    private static PointStruct ToPoint(BrainPoint point, float[] vector)
    {
        var payload = point.Payload.ToDictionary(
            pair => pair.Key,
            pair => new Value { StringValue = pair.Value ?? string.Empty });
        return new PointStruct
        {
            Id = PointId(point.PointId),
            Vectors = vector,
            Payload = { payload }
        };
    }

    private static ulong PointId(string logicalId)
    {
        var bytes = System.Security.Cryptography.SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(logicalId));
        return BitConverter.ToUInt64(bytes, 0);
    }

    private static string Read(Value value)
        => value.KindCase == Value.KindOneofCase.StringValue ? value.StringValue : value.ToString();

    private static string Truncate(string? value, int max = 240)
    {
        if (string.IsNullOrEmpty(value))
            return string.Empty;
        return value.Length <= max ? value : value[..max] + "…";
    }
}
