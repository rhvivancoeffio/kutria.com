using Qdrant.Client;
using Qdrant.Client.Grpc;
using Commerce.Application.Abstracts;
using Commerce.Infrastructure.Vectors.Common;
using static Qdrant.Client.Grpc.Conditions;

namespace Commerce.Infrastructure.Vectors.Qdrant;

public sealed class QdrantVectorStore : IVectorStore
{
    public const string CollectionName = "commerce-catalog";

    private readonly QdrantClient _client;
    private readonly SemaphoreSlim _ready = new(1, 1);
    private bool _collectionReady;

    public QdrantVectorStore(string host, int port, bool https)
    {
        _client = new QdrantClient(host, port, https);
    }

    public async Task UpsertAsync(VectorPoint point, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(point.TenantId))
        {
            throw new ArgumentException("tenantId is required.", nameof(point));
        }

        await EnsureCollectionAsync(cancellationToken);
        var id = PointId(point.Id);
        var payload = new Dictionary<string, Value>
        {
            ["tenant_id"] = point.TenantId,
            ["text"] = point.Text
        };
        foreach (var pair in point.Payload)
        {
            payload[pair.Key] = pair.Value;
        }

        var embedding = PlaceholderFromText(point.Text);
        await _client.UpsertAsync(CollectionName, [new PointStruct
        {
            Id = id,
            Vectors = embedding,
            Payload = { payload }
        }], cancellationToken: cancellationToken);
    }

    public async Task<IReadOnlyList<VectorHit>> SearchAsync(
        string tenantId,
        ReadOnlyMemory<float> vector,
        int limit,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(tenantId))
        {
            throw new ArgumentException("tenantId is required.", nameof(tenantId));
        }

        await EnsureCollectionAsync(cancellationToken);
        var query = vector.Length == PlaceholderEmbeddingGenerator.Dimensions
            ? vector.ToArray()
            : vector.Slice(0, Math.Min(vector.Length, PlaceholderEmbeddingGenerator.Dimensions)).ToArray();

        if (query.Length != PlaceholderEmbeddingGenerator.Dimensions)
        {
            var padded = new float[PlaceholderEmbeddingGenerator.Dimensions];
            query.CopyTo(padded, 0);
            query = padded;
        }

        var results = await _client.SearchAsync(
            CollectionName,
            query,
            filter: MatchKeyword("tenant_id", tenantId),
            limit: (ulong)Math.Max(limit, 0),
            cancellationToken: cancellationToken);

        return results.Select(hit =>
        {
            var payload = hit.Payload.ToDictionary(pair => pair.Key, pair => pair.Value.StringValue ?? string.Empty);
            return new VectorHit(hit.Id.ToString(), hit.Score, payload);
        }).ToList();
    }

    private async Task EnsureCollectionAsync(CancellationToken cancellationToken)
    {
        if (_collectionReady)
        {
            return;
        }

        await _ready.WaitAsync(cancellationToken);
        try
        {
            if (_collectionReady)
            {
                return;
            }

            if (!await _client.CollectionExistsAsync(CollectionName, cancellationToken))
            {
                await _client.CreateCollectionAsync(
                    CollectionName,
                    new VectorParams { Size = PlaceholderEmbeddingGenerator.Dimensions, Distance = Distance.Cosine },
                    cancellationToken: cancellationToken);
            }

            _collectionReady = true;
        }
        finally
        {
            _ready.Release();
        }
    }

    private static ulong PointId(string id)
    {
        var bytes = System.Security.Cryptography.SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(id));
        return BitConverter.ToUInt64(bytes, 0);
    }

    private static float[] PlaceholderFromText(string text)
    {
        var hash = System.Security.Cryptography.SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(text ?? string.Empty));
        var vector = new float[PlaceholderEmbeddingGenerator.Dimensions];
        for (var i = 0; i < vector.Length; i++)
        {
            vector[i] = hash[i] / 255f;
        }

        return vector;
    }
}
