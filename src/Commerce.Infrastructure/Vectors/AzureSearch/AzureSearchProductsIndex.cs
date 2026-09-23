using System.Security.Cryptography;
using System.Text;
using Azure;
using Azure.Search.Documents;
using Azure.Search.Documents.Indexes;
using Azure.Search.Documents.Models;
using Commerce.Application.Abstracts;
using Commerce.Infrastructure.Vectors.Common;
using Microsoft.Extensions.Logging;

namespace Commerce.Infrastructure.Vectors.AzureSearch;

public sealed class AzureSearchProductsIndex : ICatalogBrainIndex, ICatalogBrainWriter
{
    private readonly SearchClient _search;
    private readonly ITextEmbeddingService _embeddings;
    private readonly ILogger<AzureSearchProductsIndex> _logger;
    private readonly bool _enableSemantic;

    public AzureSearchProductsIndex(
        Uri endpoint,
        string apiKey,
        ITextEmbeddingService embeddings,
        ILogger<AzureSearchProductsIndex> logger,
        bool enableSemantic)
    {
        var credential = new AzureKeyCredential(apiKey);
        _search = new SearchClient(endpoint, AzureSearchProductsSchemas.IndexName, credential);
        _embeddings = embeddings;
        _logger = logger;
        _enableSemantic = enableSemantic;
    }

    public async Task<IReadOnlyList<CatalogBrainHit>> SearchAsync(
        string tenantId,
        string text,
        int limit,
        CancellationToken cancellationToken = default,
        IReadOnlyList<CatalogOptionFilter>? optionFilters = null)
    {
        if (string.IsNullOrWhiteSpace(tenantId) || string.IsNullOrWhiteSpace(text) || limit <= 0)
            return [];

        var optionPairs = CatalogOptionNormalizer.NormalizeFilters(optionFilters);
        _logger.LogInformation(
            "AzureSearch products vector search. TenantId={TenantId} Limit={Limit} Semantic={Semantic} OptionFilters={OptionFilters} Query={Query}",
            tenantId,
            limit,
            _enableSemantic,
            optionPairs.Count == 0 ? "-" : string.Join(',', optionPairs),
            Truncate(text));

        var options = new SearchOptions
        {
            Size = Math.Clamp(limit, 1, 50),
            Filter = BuildFilter(tenantId, optionPairs),
            QueryType = _enableSemantic ? SearchQueryType.Semantic : SearchQueryType.Simple
        };

        if (_enableSemantic)
            options.SemanticSearch = new SemanticSearchOptions
            {
                SemanticConfigurationName = AzureSearchProductsSchemas.SemanticConfig
            };

        options.VectorSearch = new VectorSearchOptions();
        options.VectorSearch.Queries.Add(new VectorizableTextQuery(text)
        {
            KNearestNeighborsCount = Math.Max(limit, 50),
            Fields = { "title_vector", "description_vector" }
        });

        try
        {
            var response = await _search.SearchAsync<ProductDocument>(text, options, cancellationToken);
            var hits = await ReadHitsAsync(response, cancellationToken);
            _logger.LogInformation(
                "AzureSearch products vector search done. TenantId={TenantId} Hits={Hits}",
                tenantId,
                hits.Count);
            return hits;
        }
        catch (RequestFailedException) when (_enableSemantic)
        {
            // Free SKU / no semantic: retry without semantic.
            options.QueryType = SearchQueryType.Simple;
            options.SemanticSearch = null;
            var response = await _search.SearchAsync<ProductDocument>(text, options, cancellationToken);
            var hits = await ReadHitsAsync(response, cancellationToken);
            _logger.LogInformation(
                "AzureSearch products vector search done (no semantic). TenantId={TenantId} Hits={Hits}",
                tenantId,
                hits.Count);
            return hits;
        }
    }

    public Task UpsertAsync(string tenantId, string sku, string meaning, CancellationToken cancellationToken = default)
        => UpsertAsync(
            new CatalogProductDocument(tenantId, sku, meaning, meaning, null, 0, true, null),
            cancellationToken);

    public async Task UpsertAsync(CatalogProductDocument document, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(document.TenantId) || string.IsNullOrWhiteSpace(document.Sku))
            throw new ArgumentException("tenantId and sku are required.");

        var title = string.IsNullOrWhiteSpace(document.Title) ? document.Sku : document.Title;
        var description = string.IsNullOrWhiteSpace(document.Description) ? title : document.Description;

        _logger.LogInformation(
            "AzureSearch products upsert. TenantId={TenantId} Sku={Sku} DocId={DocId} ProductId={ProductId} SkuId={SkuId} SellerId={SellerId} Title={Title} Description={Description} Category={Category} Brand={Brand} Seller={Seller} Price={Price} IsActive={IsActive} OptionPairs={OptionPairs} ImageUrl={ImageUrl}",
            document.TenantId,
            document.Sku,
            DocumentId(document.TenantId, document.Sku),
            document.ProductId,
            document.SkuId,
            document.SellerId,
            Truncate(title),
            Truncate(description),
            document.Category,
            document.Brand,
            document.Seller,
            document.Price,
            document.IsActive,
            document.OptionPairs is { Count: > 0 } ? string.Join(',', document.OptionPairs) : "-",
            Truncate(document.ImageUrl, 200));

        var titleVec = await _embeddings.EmbedAsync(title, cancellationToken);
        var descVec = await _embeddings.EmbedAsync(description, cancellationToken);

        // Image vectors live on separate doc_type=image documents (catalog-image-vector queue).
        var optionKeys = document.OptionKeys?.Where(x => !string.IsNullOrWhiteSpace(x)).Distinct(StringComparer.OrdinalIgnoreCase).ToArray()
            ?? Array.Empty<string>();
        var optionPairs = document.OptionPairs?.Where(x => !string.IsNullOrWhiteSpace(x)).Distinct(StringComparer.OrdinalIgnoreCase).ToArray()
            ?? Array.Empty<string>();

        var doc = new ProductDocument
        {
            Id = DocumentId(document.TenantId, document.Sku),
            TenantId = document.TenantId,
            Sku = document.Sku,
            ProductId = document.ProductId ?? string.Empty,
            SkuId = document.SkuId ?? document.Sku,
            SellerId = document.SellerId ?? string.Empty,
            Title = title,
            Description = description,
            Category = document.Category ?? string.Empty,
            Brand = document.Brand ?? string.Empty,
            Seller = document.Seller ?? string.Empty,
            Price = (double)document.Price,
            Stock = document.Stock,
            IsActive = document.IsActive,
            ImageUrl = document.ImageUrl ?? string.Empty,
            ImageStatus = CatalogImageStatuses.None,
            DocType = CatalogDocTypes.Sku,
            OptionKeys = optionKeys,
            OptionPairs = optionPairs,
            TitleVector = titleVec,
            DescriptionVector = descVec,
            ImageVector = null
        };

        _logger.LogInformation(
            "AzureSearch products upsert vectors. TenantId={TenantId} Sku={Sku} TitleDims={TitleDims} DescriptionDims={DescriptionDims} DocType={DocType} ImageVectorOmitted=true",
            document.TenantId,
            document.Sku,
            titleVec.Length,
            descVec.Length,
            CatalogDocTypes.Sku);

        await _search.MergeOrUploadDocumentsAsync([doc], cancellationToken: cancellationToken);
        _logger.LogInformation(
            "AzureSearch products upsert done. TenantId={TenantId} Sku={Sku}",
            document.TenantId,
            document.Sku);
    }

    public async Task UpsertImageDocumentAsync(
        CatalogImageDocument document,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(document.TenantId) || string.IsNullOrWhiteSpace(document.ImageUrl))
            throw new ArgumentException("tenantId and imageUrl are required.");
        if (document.ImageVector is not { Count: > 0 })
            throw new ArgumentException("imageVector is required.");

        var normalizedUrl = CatalogImageUrl.Normalize(document.ImageUrl) ?? document.ImageUrl.Trim();
        var sku = string.IsNullOrWhiteSpace(document.Sku) ? normalizedUrl : document.Sku.Trim();
        var title = string.IsNullOrWhiteSpace(document.Title) ? sku : document.Title;

        var doc = new ProductDocument
        {
            Id = ImageDocumentId(document.TenantId, normalizedUrl),
            TenantId = document.TenantId,
            Sku = sku,
            ProductId = document.ProductId ?? string.Empty,
            SkuId = document.SkuId ?? sku,
            SellerId = document.SellerId ?? string.Empty,
            Title = title,
            Description = title,
            Category = string.Empty,
            Brand = document.Brand ?? string.Empty,
            Seller = document.Seller ?? string.Empty,
            Price = (double)document.Price,
            Stock = document.Stock,
            IsActive = document.IsActive,
            ImageUrl = normalizedUrl,
            ImageStatus = CatalogImageStatuses.Indexed,
            DocType = CatalogDocTypes.Image,
            OptionKeys = Array.Empty<string>(),
            OptionPairs = Array.Empty<string>(),
            TitleVector = null,
            DescriptionVector = null,
            ImageVector = document.ImageVector as float[] ?? document.ImageVector.ToArray()
        };

        _logger.LogInformation(
            "AzureSearch products image-doc upsert. TenantId={TenantId} ProductId={ProductId} Sku={Sku} DocId={DocId} ImageDims={ImageDims} ImageUrl={ImageUrl}",
            document.TenantId,
            document.ProductId,
            sku,
            doc.Id,
            doc.ImageVector!.Count,
            Truncate(normalizedUrl, 200));

        await _search.MergeOrUploadDocumentsAsync([doc], cancellationToken: cancellationToken);
        _logger.LogInformation(
            "AzureSearch products image-doc upsert done. TenantId={TenantId} DocId={DocId}",
            document.TenantId,
            doc.Id);
    }

    private static async Task<IReadOnlyList<CatalogBrainHit>> ReadHitsAsync(
        Response<SearchResults<ProductDocument>> response,
        CancellationToken cancellationToken)
    {
        var hits = new List<CatalogBrainHit>();
        await foreach (var result in response.Value.GetResultsAsync().WithCancellation(cancellationToken))
        {
            var doc = result.Document;
            if (doc is null || string.IsNullOrWhiteSpace(doc.Sku))
                continue;
            hits.Add(new CatalogBrainHit(
                doc.Sku,
                doc.Description ?? doc.Title ?? string.Empty,
                result.Score.HasValue ? (float)result.Score.Value : 0f,
                Title: string.IsNullOrWhiteSpace(doc.Title) ? null : doc.Title,
                Price: doc.Price > 0 ? (decimal)doc.Price : null,
                IsActive: doc.IsActive,
                ImageUrl: string.IsNullOrWhiteSpace(doc.ImageUrl) ? null : doc.ImageUrl,
                Brand: string.IsNullOrWhiteSpace(doc.Brand) ? null : doc.Brand,
                Seller: string.IsNullOrWhiteSpace(doc.Seller) ? null : doc.Seller,
                Stock: doc.Stock,
                ProductId: string.IsNullOrWhiteSpace(doc.ProductId) ? null : doc.ProductId,
                SkuId: string.IsNullOrWhiteSpace(doc.SkuId) ? doc.Sku : doc.SkuId,
                SellerId: string.IsNullOrWhiteSpace(doc.SellerId) ? null : doc.SellerId,
                OptionPairs: doc.OptionPairs is { Count: > 0 } ? doc.OptionPairs : null));
        }

        return hits;
    }

    private static string BuildFilter(string tenantId, IReadOnlyList<string> optionPairs)
    {
        var filter = $"tenant_id eq '{Escape(tenantId)}' and doc_type eq '{CatalogDocTypes.Sku}'";
        foreach (var pair in optionPairs)
            filter += $" and option_pairs/any(p: p eq '{Escape(pair)}')";
        return filter;
    }

    private static string Escape(string value) => value.Replace("'", "''", StringComparison.Ordinal);

    /// <summary>
    /// Azure AI Search keys may only contain letters, digits, underscore, dash, or '='.
    /// Hash tenant|sku so SKUs with '|', spaces, etc. stay valid and stable.
    /// </summary>
    internal static string DocumentId(string tenantId, string sku)
    {
        var raw = $"{tenantId}|{sku}";
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(raw))).ToLowerInvariant();
    }

    /// <summary>One image-angle document per unique URL within a tenant.</summary>
    internal static string ImageDocumentId(string tenantId, string normalizedImageUrl)
    {
        var raw = $"{tenantId}|img|{normalizedImageUrl}";
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(raw))).ToLowerInvariant();
    }

    private static string Truncate(string? value, int max = 240)
    {
        if (string.IsNullOrEmpty(value))
            return string.Empty;
        return value.Length <= max ? value : value[..max] + "…";
    }

    private sealed class ProductDocument
    {
        [System.Text.Json.Serialization.JsonPropertyName("id")]
        public string Id { get; set; } = string.Empty;

        [System.Text.Json.Serialization.JsonPropertyName("tenant_id")]
        public string TenantId { get; set; } = string.Empty;

        [System.Text.Json.Serialization.JsonPropertyName("sku")]
        public string Sku { get; set; } = string.Empty;

        [System.Text.Json.Serialization.JsonPropertyName("product_id")]
        public string ProductId { get; set; } = string.Empty;

        [System.Text.Json.Serialization.JsonPropertyName("sku_id")]
        public string SkuId { get; set; } = string.Empty;

        [System.Text.Json.Serialization.JsonPropertyName("seller_id")]
        public string SellerId { get; set; } = string.Empty;

        [System.Text.Json.Serialization.JsonPropertyName("title")]
        public string Title { get; set; } = string.Empty;

        [System.Text.Json.Serialization.JsonPropertyName("description")]
        public string? Description { get; set; }

        [System.Text.Json.Serialization.JsonPropertyName("category")]
        public string Category { get; set; } = string.Empty;

        [System.Text.Json.Serialization.JsonPropertyName("brand")]
        public string Brand { get; set; } = string.Empty;

        [System.Text.Json.Serialization.JsonPropertyName("seller")]
        public string Seller { get; set; } = string.Empty;

        [System.Text.Json.Serialization.JsonPropertyName("price")]
        public double Price { get; set; }

        [System.Text.Json.Serialization.JsonPropertyName("stock")]
        public int Stock { get; set; }

        [System.Text.Json.Serialization.JsonPropertyName("is_active")]
        public bool IsActive { get; set; }

        [System.Text.Json.Serialization.JsonPropertyName("image_url")]
        public string ImageUrl { get; set; } = string.Empty;

        [System.Text.Json.Serialization.JsonPropertyName("image_status")]
        public string ImageStatus { get; set; } = CatalogImageStatuses.None;

        [System.Text.Json.Serialization.JsonPropertyName("doc_type")]
        public string DocType { get; set; } = CatalogDocTypes.Sku;

        [System.Text.Json.Serialization.JsonPropertyName("option_keys")]
        public IReadOnlyList<string> OptionKeys { get; set; } = Array.Empty<string>();

        [System.Text.Json.Serialization.JsonPropertyName("option_pairs")]
        public IReadOnlyList<string> OptionPairs { get; set; } = Array.Empty<string>();

        [System.Text.Json.Serialization.JsonPropertyName("title_vector")]
        [System.Text.Json.Serialization.JsonIgnore(
            Condition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull)]
        public IReadOnlyList<float>? TitleVector { get; set; }

        [System.Text.Json.Serialization.JsonPropertyName("description_vector")]
        [System.Text.Json.Serialization.JsonIgnore(
            Condition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull)]
        public IReadOnlyList<float>? DescriptionVector { get; set; }

        [System.Text.Json.Serialization.JsonPropertyName("image_vector")]
        [System.Text.Json.Serialization.JsonIgnore(
            Condition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull)]
        public IReadOnlyList<float>? ImageVector { get; set; }
    }
}

public sealed class AzureSearchProductsBootstrapper : IVectorIndexBootstrapper
{
    private readonly Uri _endpoint;
    private readonly string _apiKey;
    private readonly string _openAiEndpoint;
    private readonly string _openAiApiKey;
    private readonly string _embeddingDeployment;
    private readonly bool _deleteIndexOnBootstrap;
    private readonly ILogger<AzureSearchProductsBootstrapper> _logger;

    public AzureSearchProductsBootstrapper(
        Uri endpoint,
        string apiKey,
        string openAiEndpoint,
        string openAiApiKey,
        string embeddingDeployment,
        ILogger<AzureSearchProductsBootstrapper> logger,
        bool deleteIndexOnBootstrap = false)
    {
        _endpoint = endpoint;
        _apiKey = apiKey;
        _openAiEndpoint = openAiEndpoint;
        _openAiApiKey = openAiApiKey;
        _embeddingDeployment = embeddingDeployment;
        _logger = logger;
        _deleteIndexOnBootstrap = deleteIndexOnBootstrap;
    }

    public async Task EnsureAsync(CancellationToken cancellationToken = default)
    {
        var indexName = AzureSearchProductsSchemas.IndexName;
        var indexClient = new SearchIndexClient(_endpoint, new AzureKeyCredential(_apiKey));

        _logger.LogInformation(
            "AzureSearch products bootstrap start. Index={Index} DeleteIndexOnBootstrap={Delete}",
            indexName,
            _deleteIndexOnBootstrap);

        var existedBefore = await IndexExistsAsync(indexClient, indexName, cancellationToken);

        if (_deleteIndexOnBootstrap)
        {
            if (existedBefore)
            {
                await indexClient.DeleteIndexAsync(indexName, cancellationToken);
                _logger.LogWarning(
                    "AzureSearch products index deleted for recreate. Index={Index}",
                    indexName);
                existedBefore = false;
            }
            else
            {
                _logger.LogInformation(
                    "AzureSearch products delete skipped (index did not exist). Index={Index}",
                    indexName);
            }
        }
        else
        {
            _logger.LogInformation(
                "AzureSearch products delete skipped (DeleteIndexOnBootstrap=false). Index={Index} Existed={Existed}",
                indexName,
                existedBefore);
        }

        var index = AzureSearchProductsSchemas.CreateProductsIndex(
            _openAiEndpoint,
            _openAiApiKey,
            _embeddingDeployment);
        await indexClient.CreateOrUpdateIndexAsync(index, cancellationToken: cancellationToken);

        _logger.LogInformation(
            "AzureSearch products bootstrap done. Index={Index} Action={Action}",
            indexName,
            existedBefore ? "create_or_update_existing" : "create_new");
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
}
