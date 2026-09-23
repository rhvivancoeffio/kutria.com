using System.Collections.Concurrent;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json.Serialization;
using Azure;
using Azure.Search.Documents;
using Azure.Search.Documents.Indexes;
using Azure.Search.Documents.Models;
using Commerce.Application.Abstracts;
using Commerce.Infrastructure.Vectors.Common;
using Microsoft.Extensions.Logging;

namespace Commerce.Infrastructure.Vectors.AzureSearch;

/// <summary>
/// IBrainIndex on Azure AI Search (same Free service as products). Collection name → index name.
/// </summary>
public sealed class AzureSearchBrainIndex : IBrainIndex
{
    private static readonly HashSet<string> KnownFields = new(StringComparer.Ordinal)
    {
        "tenant_id", "workspace_id", "point_id", "title", "content", "type", "language", "job_id", "policy_id",
        "is_evaluated", "applicable_sellers", "effective_from", "effective_to", "source_url",
        "chunk_id", "point_key"
    };

    private readonly Uri _endpoint;
    private readonly AzureKeyCredential _credential;
    private readonly ITextEmbeddingService _embeddings;
    private readonly ILogger<AzureSearchBrainIndex> _logger;
    private readonly ConcurrentDictionary<string, byte> _ensured = new(StringComparer.Ordinal);
    private readonly SemaphoreSlim _ready = new(1, 1);
    private readonly string _openAiEndpoint;
    private readonly string _openAiApiKey;
    private readonly string _embeddingDeployment;
    private readonly bool _deleteIndexOnBootstrap;

    public AzureSearchBrainIndex(
        Uri endpoint,
        string apiKey,
        ITextEmbeddingService embeddings,
        ILogger<AzureSearchBrainIndex> logger,
        string openAiEndpoint,
        string openAiApiKey,
        string embeddingDeployment,
        bool deleteIndexOnBootstrap = false)
    {
        _endpoint = endpoint;
        _credential = new AzureKeyCredential(apiKey);
        _embeddings = embeddings;
        _logger = logger;
        _openAiEndpoint = openAiEndpoint;
        _openAiApiKey = openAiApiKey;
        _embeddingDeployment = embeddingDeployment;
        _deleteIndexOnBootstrap = deleteIndexOnBootstrap;
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

        var indexName = NormalizeIndexName(search.CollectionName);
        var indexClient = new SearchIndexClient(_endpoint, _credential);
        if (!await IndexExistsAsync(indexClient, indexName, cancellationToken))
            return [];

        _logger.LogInformation(
            "AzureSearch brain vector search. Index={Index} TenantId={TenantId} Limit={Limit} Query={Query}",
            indexName,
            search.TenantId,
            search.Limit,
            Truncate(search.Text));

        var vector = await _embeddings.EmbedAsync(search.Text, cancellationToken);
        if (vector.Length == 0)
            return [];

        var client = new SearchClient(_endpoint, indexName, _credential);
        var filter = BuildFilter(
            new Dictionary<string, string> { ["tenant_id"] = search.TenantId },
            search.MustMatch,
            except: null);

        var options = new SearchOptions
        {
            Size = Math.Clamp(search.Limit, 1, 50),
            Filter = filter,
            VectorSearch = new VectorSearchOptions()
        };
        options.VectorSearch.Queries.Add(new VectorizedQuery(vector)
        {
            KNearestNeighborsCount = Math.Max(search.Limit, 20),
            Fields = { AzureSearchBrainSchemas.ContentVector }
        });

        var response = await client.SearchAsync<BrainDocument>(search.Text, options, cancellationToken);
        var hits = new List<BrainHit>();
        await foreach (var result in response.Value.GetResultsAsync().WithCancellation(cancellationToken))
        {
            if (result.Document is null)
                continue;
            hits.Add(new BrainHit(
                result.Score.HasValue ? (float)result.Score.Value : 0f,
                result.Document.ToPayload()));
        }

        _logger.LogInformation(
            "AzureSearch brain vector search done. Index={Index} TenantId={TenantId} QueryDims={Dims} Hits={Hits}",
            indexName,
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
            throw new ArgumentException("A collection and at least one point are required.");

        var indexName = NormalizeIndexName(collectionName);
        await EnsureIndexAsync(indexName, cancellationToken);
        var client = new SearchClient(_endpoint, indexName, _credential);

        _logger.LogInformation(
            "AzureSearch brain upsert. Index={Index} Points={Points}",
            indexName,
            points.Count);

        var docs = new List<BrainDocument>(points.Count);
        foreach (var point in points)
        {
            var vector = await _embeddings.EmbedAsync(point.TextToEmbed, cancellationToken);
            _logger.LogInformation(
                "AzureSearch brain upsert point. Index={Index} PointId={PointId} VectorDims={Dims} TextToEmbed={Text} PayloadKeys={PayloadKeys}",
                indexName,
                point.PointId,
                vector.Length,
                Truncate(point.TextToEmbed),
                string.Join(',', point.Payload.Keys));
            docs.Add(BrainDocument.FromPoint(point, vector));
        }

        await client.MergeOrUploadDocumentsAsync(docs, cancellationToken: cancellationToken);
        _logger.LogInformation(
            "AzureSearch brain upsert done. Index={Index} Points={Points}",
            indexName,
            docs.Count);
    }

    public async Task SetPayloadAsync(
        string collectionName,
        IReadOnlyDictionary<string, string> match,
        IReadOnlyDictionary<string, string> payload,
        CancellationToken cancellationToken = default)
    {
        var indexName = NormalizeIndexName(collectionName);
        var indexClient = new SearchIndexClient(_endpoint, _credential);
        if (!await IndexExistsAsync(indexClient, indexName, cancellationToken))
            return;

        var client = new SearchClient(_endpoint, indexName, _credential);
        var docs = await FindDocumentsAsync(client, match, except: null, cancellationToken);
        if (docs.Count == 0)
            return;

        foreach (var doc in docs)
            doc.ApplyPayload(payload);

        // Merge only — omit content_vector so embeddings are preserved.
        await client.MergeDocumentsAsync(docs, cancellationToken: cancellationToken);
    }

    public async Task DeleteMatchingAsync(
        string collectionName,
        IReadOnlyDictionary<string, string> match,
        IReadOnlyDictionary<string, string>? except,
        CancellationToken cancellationToken = default)
    {
        var indexName = NormalizeIndexName(collectionName);
        var indexClient = new SearchIndexClient(_endpoint, _credential);
        if (!await IndexExistsAsync(indexClient, indexName, cancellationToken))
            return;

        var client = new SearchClient(_endpoint, indexName, _credential);
        var docs = await FindDocumentsAsync(client, match, except, cancellationToken);
        if (docs.Count == 0)
            return;

        await client.DeleteDocumentsAsync(
            "id",
            docs.Select(d => d.Id),
            cancellationToken: cancellationToken);
    }

    public async Task EnsureStartupIndexesAsync(CancellationToken cancellationToken = default)
    {
        foreach (var name in AzureSearchBrainSchemas.StartupIndexNames)
            await EnsureIndexAsync(name, createOrUpdate: true, cancellationToken);
    }

    private async Task EnsureIndexAsync(
        string indexName,
        CancellationToken cancellationToken)
        => await EnsureIndexAsync(indexName, createOrUpdate: false, cancellationToken);

    private async Task EnsureIndexAsync(
        string indexName,
        bool createOrUpdate,
        CancellationToken cancellationToken)
    {
        if (_ensured.ContainsKey(indexName))
            return;

        await _ready.WaitAsync(cancellationToken);
        try
        {
            if (_ensured.ContainsKey(indexName))
                return;

            var indexClient = new SearchIndexClient(_endpoint, _credential);
            if (_deleteIndexOnBootstrap)
            {
                try
                {
                    await indexClient.DeleteIndexAsync(indexName, cancellationToken);
                }
                catch (RequestFailedException ex) when (ex.Status == 404)
                {
                }
            }

            var exists = await IndexExistsAsync(indexClient, indexName, cancellationToken);
            if (!exists || createOrUpdate)
            {
                var index = AzureSearchBrainSchemas.CreateBrainIndex(
                    indexName,
                    _openAiEndpoint,
                    _openAiApiKey,
                    _embeddingDeployment);
                await indexClient.CreateOrUpdateIndexAsync(index, cancellationToken: cancellationToken);
            }

            _ensured[indexName] = 0;
        }
        finally
        {
            _ready.Release();
        }
    }

    private static async Task<bool> IndexExistsAsync(
        SearchIndexClient indexClient,
        string indexName,
        CancellationToken cancellationToken)
    {
        try
        {
            await indexClient.GetIndexAsync(indexName, cancellationToken);
            return true;
        }
        catch (RequestFailedException ex) when (ex.Status == 404)
        {
            return false;
        }
    }

    private static async Task<List<BrainDocument>> FindDocumentsAsync(
        SearchClient client,
        IReadOnlyDictionary<string, string> match,
        IReadOnlyDictionary<string, string>? except,
        CancellationToken cancellationToken)
    {
        var filter = BuildFilter(match, extraMust: null, except);
        if (string.IsNullOrWhiteSpace(filter))
            return [];

        var found = new List<BrainDocument>();
        var options = new SearchOptions
        {
            Size = 1000,
            Filter = filter,
            IncludeTotalCount = false
        };
        options.Select.Add("id");
        foreach (var field in KnownFields)
            options.Select.Add(field);

        var response = await client.SearchAsync<BrainDocument>("*", options, cancellationToken);
        await foreach (var result in response.Value.GetResultsAsync().WithCancellation(cancellationToken))
        {
            if (result.Document is not null)
                found.Add(result.Document);
        }

        return found;
    }

    private static string? BuildFilter(
        IReadOnlyDictionary<string, string> match,
        IReadOnlyDictionary<string, string>? extraMust,
        IReadOnlyDictionary<string, string>? except)
    {
        var parts = new List<string>();
        AppendEquals(parts, match);
        if (extraMust is not null)
            AppendEquals(parts, extraMust);
        if (except is not null)
        {
            foreach (var pair in except)
            {
                if (string.IsNullOrWhiteSpace(pair.Value) || !KnownFields.Contains(pair.Key))
                    continue;
                parts.Add($"{pair.Key} ne '{Escape(pair.Value)}'");
            }
        }

        return parts.Count == 0 ? null : string.Join(" and ", parts);
    }

    private static void AppendEquals(List<string> parts, IReadOnlyDictionary<string, string> map)
    {
        foreach (var pair in map)
        {
            if (string.IsNullOrWhiteSpace(pair.Value) || !KnownFields.Contains(pair.Key))
                continue;
            parts.Add($"{pair.Key} eq '{Escape(pair.Value)}'");
        }
    }

    private static string NormalizeIndexName(string collectionName)
        => collectionName.Trim().ToLowerInvariant().Replace('_', '-');

    private static string Escape(string value) => value.Replace("'", "''", StringComparison.Ordinal);

    private static string Truncate(string? value, int max = 240)
    {
        if (string.IsNullOrEmpty(value))
            return string.Empty;
        return value.Length <= max ? value : value[..max] + "…";
    }

    internal static string DocumentKey(string pointId)
    {
        var hash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(pointId))).ToLowerInvariant();
        return hash;
    }

    private sealed class BrainDocument
    {
        [JsonPropertyName("id")]
        public string Id { get; set; } = string.Empty;

        [JsonPropertyName("tenant_id")]
        public string TenantId { get; set; } = string.Empty;

        [JsonPropertyName("workspace_id")]
        public string WorkspaceId { get; set; } = string.Empty;

        [JsonPropertyName("point_id")]
        public string PointId { get; set; } = string.Empty;

        [JsonPropertyName("title")]
        public string Title { get; set; } = string.Empty;

        [JsonPropertyName("content")]
        public string Content { get; set; } = string.Empty;

        [JsonPropertyName("type")]
        public string Type { get; set; } = string.Empty;

        [JsonPropertyName("language")]
        public string Language { get; set; } = string.Empty;

        [JsonPropertyName("job_id")]
        public string JobId { get; set; } = string.Empty;

        [JsonPropertyName("policy_id")]
        public string PolicyId { get; set; } = string.Empty;

        [JsonPropertyName("is_evaluated")]
        public string IsEvaluated { get; set; } = string.Empty;

        [JsonPropertyName("applicable_sellers")]
        public string ApplicableSellers { get; set; } = string.Empty;

        [JsonPropertyName("effective_from")]
        public string EffectiveFrom { get; set; } = string.Empty;

        [JsonPropertyName("effective_to")]
        public string EffectiveTo { get; set; } = string.Empty;

        [JsonPropertyName("source_url")]
        public string SourceUrl { get; set; } = string.Empty;

        [JsonPropertyName("chunk_id")]
        public string ChunkId { get; set; } = string.Empty;

        [JsonPropertyName("point_key")]
        public string PointKey { get; set; } = string.Empty;

        [JsonPropertyName("content_vector")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public IReadOnlyList<float>? ContentVector { get; set; }

        public static BrainDocument FromPoint(BrainPoint point, float[] vector)
        {
            var payload = point.Payload;
            var content = Get(payload, "content");
            return new BrainDocument
            {
                Id = DocumentKey(point.PointId),
                PointId = point.PointId,
                TenantId = Get(payload, "tenant_id"),
                WorkspaceId = Get(payload, "workspace_id"),
                Title = Get(payload, "title"),
                Content = content.Length > 0 ? content : point.TextToEmbed,
                Type = Get(payload, "type"),
                Language = Get(payload, "language"),
                JobId = Get(payload, "job_id"),
                PolicyId = Get(payload, "policy_id"),
                IsEvaluated = Get(payload, "is_evaluated"),
                ApplicableSellers = Get(payload, "applicable_sellers"),
                EffectiveFrom = Get(payload, "effective_from"),
                EffectiveTo = Get(payload, "effective_to"),
                SourceUrl = Get(payload, "source_url"),
                ChunkId = Get(payload, "chunk_id"),
                PointKey = Get(payload, "point_key"),
                ContentVector = vector
            };
        }

        public void ApplyPayload(IReadOnlyDictionary<string, string> payload)
        {
            foreach (var pair in payload)
            {
                if (string.IsNullOrWhiteSpace(pair.Key))
                    continue;
                switch (pair.Key)
                {
                    case "tenant_id": TenantId = pair.Value ?? string.Empty; break;
                    case "workspace_id": WorkspaceId = pair.Value ?? string.Empty; break;
                    case "point_id": PointId = pair.Value ?? string.Empty; break;
                    case "title": Title = pair.Value ?? string.Empty; break;
                    case "content": Content = pair.Value ?? string.Empty; break;
                    case "type": Type = pair.Value ?? string.Empty; break;
                    case "language": Language = pair.Value ?? string.Empty; break;
                    case "job_id": JobId = pair.Value ?? string.Empty; break;
                    case "policy_id": PolicyId = pair.Value ?? string.Empty; break;
                    case "is_evaluated": IsEvaluated = pair.Value ?? string.Empty; break;
                    case "applicable_sellers": ApplicableSellers = pair.Value ?? string.Empty; break;
                    case "effective_from": EffectiveFrom = pair.Value ?? string.Empty; break;
                    case "effective_to": EffectiveTo = pair.Value ?? string.Empty; break;
                    case "source_url": SourceUrl = pair.Value ?? string.Empty; break;
                    case "chunk_id": ChunkId = pair.Value ?? string.Empty; break;
                    case "point_key": PointKey = pair.Value ?? string.Empty; break;
                }
            }
        }

        public Dictionary<string, string> ToPayload() => new(StringComparer.Ordinal)
        {
            ["tenant_id"] = TenantId,
            ["workspace_id"] = WorkspaceId,
            ["point_id"] = PointId,
            ["title"] = Title,
            ["content"] = Content,
            ["content_enriched"] = Content,
            ["type"] = Type,
            ["language"] = Language,
            ["job_id"] = JobId,
            ["policy_id"] = PolicyId,
            ["is_evaluated"] = IsEvaluated,
            ["applicable_sellers"] = ApplicableSellers,
            ["effective_from"] = EffectiveFrom,
            ["effective_to"] = EffectiveTo,
            ["source_url"] = SourceUrl,
            ["chunk_id"] = ChunkId,
            ["point_key"] = PointKey
        };

        private static string Get(IReadOnlyDictionary<string, string> payload, string key)
            => payload.TryGetValue(key, out var value) ? value ?? string.Empty : string.Empty;
    }
}

public sealed class AzureSearchBrainBootstrapper : IVectorIndexBootstrapper
{
    private readonly AzureSearchBrainIndex _index;

    public AzureSearchBrainBootstrapper(AzureSearchBrainIndex index) => _index = index;

    public Task EnsureAsync(CancellationToken cancellationToken = default)
        => _index.EnsureStartupIndexesAsync(cancellationToken);
}

public sealed class AzureSearchCompositeBootstrapper : IVectorIndexBootstrapper
{
    private readonly IReadOnlyList<IVectorIndexBootstrapper> _bootstrappers;

    public AzureSearchCompositeBootstrapper(params IVectorIndexBootstrapper[] bootstrappers)
        => _bootstrappers = bootstrappers;

    public async Task EnsureAsync(CancellationToken cancellationToken = default)
    {
        foreach (var bootstrapper in _bootstrappers)
            await bootstrapper.EnsureAsync(cancellationToken);
    }
}
