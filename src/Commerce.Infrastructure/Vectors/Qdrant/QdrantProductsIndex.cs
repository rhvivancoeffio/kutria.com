using Microsoft.Extensions.Logging;
using Qdrant.Client;
using Qdrant.Client.Grpc;
using Commerce.Application.Abstracts;
using Commerce.Infrastructure.Vectors.Common;
using static Qdrant.Client.Grpc.Conditions;

namespace Commerce.Infrastructure.Vectors.Qdrant;

public sealed class QdrantProductsIndex : ICatalogBrainIndex, ICatalogBrainWriter
{
    public const string CollectionName = "products";
    public const string VectorTitle = "title";
    public const string VectorDescription = "description";
    public const string VectorImage = "image";
    public const ulong DefaultVectorSize = 1536;

    private readonly QdrantClient _client;
    private readonly ITextEmbeddingService _embeddings;
    private readonly ILogger<QdrantProductsIndex> _logger;
    private readonly bool _deleteCollectionOnBootstrap;
    private readonly SemaphoreSlim _ready = new(1, 1);
    private bool _collectionReady;

    public QdrantProductsIndex(
        string host,
        int port,
        bool https,
        ITextEmbeddingService embeddings,
        ILogger<QdrantProductsIndex> logger,
        bool deleteCollectionOnBootstrap = false)
    {
        _client = new QdrantClient(host, port, https);
        _embeddings = embeddings;
        _logger = logger;
        _deleteCollectionOnBootstrap = deleteCollectionOnBootstrap;
    }

    public async Task EnsureCollectionAsync(CancellationToken cancellationToken = default)
    {
        if (_collectionReady)
        {
            _logger.LogDebug(
                "Qdrant products bootstrap skipped (already ready). Collection={Collection}",
                CollectionName);
            return;
        }

        await _ready.WaitAsync(cancellationToken);
        try
        {
            if (_collectionReady)
            {
                _logger.LogDebug(
                    "Qdrant products bootstrap skipped (already ready after wait). Collection={Collection}",
                    CollectionName);
                return;
            }

            _logger.LogInformation(
                "Qdrant products bootstrap start. Collection={Collection} DeleteCollectionOnBootstrap={Delete}",
                CollectionName,
                _deleteCollectionOnBootstrap);

            var existedBefore = await _client.CollectionExistsAsync(CollectionName, cancellationToken);

            if (_deleteCollectionOnBootstrap)
            {
                if (existedBefore)
                {
                    await _client.DeleteCollectionAsync(CollectionName, cancellationToken: cancellationToken);
                    _logger.LogWarning(
                        "Qdrant products collection deleted for schema recreate. Collection={Collection}",
                        CollectionName);
                    existedBefore = false;
                }
                else
                {
                    _logger.LogInformation(
                        "Qdrant products delete skipped (collection did not exist). Collection={Collection}",
                        CollectionName);
                }
            }
            else
            {
                _logger.LogInformation(
                    "Qdrant products delete skipped (DeleteCollectionOnBootstrap=false). Collection={Collection} Existed={Existed}",
                    CollectionName,
                    existedBefore);
            }

            if (!await _client.CollectionExistsAsync(CollectionName, cancellationToken))
            {
                var map = new VectorParamsMap();
                map.Map[VectorTitle] = new VectorParams { Size = DefaultVectorSize, Distance = Distance.Cosine };
                map.Map[VectorDescription] = new VectorParams { Size = DefaultVectorSize, Distance = Distance.Cosine };
                map.Map[VectorImage] = new VectorParams { Size = DefaultVectorSize, Distance = Distance.Cosine };
                await _client.CreateCollectionAsync(CollectionName, map, cancellationToken: cancellationToken);

                await _client.CreatePayloadIndexAsync(
                    CollectionName, "tenant_id", PayloadSchemaType.Keyword, cancellationToken: cancellationToken);
                await _client.CreatePayloadIndexAsync(
                    CollectionName, "is_active", PayloadSchemaType.Keyword, cancellationToken: cancellationToken);
                await _client.CreatePayloadIndexAsync(
                    CollectionName, "category", PayloadSchemaType.Keyword, cancellationToken: cancellationToken);
                await _client.CreatePayloadIndexAsync(
                    CollectionName, "option_keys", PayloadSchemaType.Keyword, cancellationToken: cancellationToken);
                await _client.CreatePayloadIndexAsync(
                    CollectionName, "option_pairs", PayloadSchemaType.Keyword, cancellationToken: cancellationToken);
                await _client.CreatePayloadIndexAsync(
                    CollectionName, "image_status", PayloadSchemaType.Keyword, cancellationToken: cancellationToken);
                await _client.CreatePayloadIndexAsync(
                    CollectionName, "doc_type", PayloadSchemaType.Keyword, cancellationToken: cancellationToken);

                _logger.LogInformation(
                    "Qdrant products collection created. Collection={Collection}",
                    CollectionName);
            }
            else
            {
                _logger.LogInformation(
                    "Qdrant products collection create skipped (already exists). Collection={Collection}",
                    CollectionName);
            }

            _collectionReady = true;
            _logger.LogInformation(
                "Qdrant products bootstrap done. Collection={Collection} Action={Action}",
                CollectionName,
                existedBefore ? "reuse_existing" : "create_new");
        }
        finally
        {
            _ready.Release();
        }
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

        if (!await _client.CollectionExistsAsync(CollectionName, cancellationToken))
            return [];

        var optionPairs = CatalogOptionNormalizer.NormalizeFilters(optionFilters);
        _logger.LogInformation(
            "Qdrant products vector search. TenantId={TenantId} Limit={Limit} OptionFilters={OptionFilters} Query={Query}",
            tenantId,
            limit,
            optionPairs.Count == 0 ? "-" : string.Join(',', optionPairs),
            Truncate(text));

        var query = await _embeddings.EmbedAsync(text, cancellationToken);
        if (query.Length == 0)
            return [];

        var filter = TenantActiveFilter(tenantId, CatalogDocTypes.Sku, optionPairs);
        var take = (ulong)Math.Max(limit, 50);

        var titleTask = _client.SearchAsync(
            CollectionName,
            query,
            filter: filter,
            limit: take,
            vectorName: VectorTitle,
            cancellationToken: cancellationToken);
        var descTask = _client.SearchAsync(
            CollectionName,
            query,
            filter: filter,
            limit: take,
            vectorName: VectorDescription,
            cancellationToken: cancellationToken);

        await Task.WhenAll(titleTask, descTask);

        var titleRaw = titleTask.Result.Select(ToScored).Where(x => !string.IsNullOrWhiteSpace(x.Id)).ToList();
        var descRaw = descTask.Result.Select(ToScored).Where(x => !string.IsNullOrWhiteSpace(x.Id)).ToList();
        var merged = RrfFusion.Merge(
            [titleRaw.Select(x => (x.Id, x.Score)).ToList(), descRaw.Select(x => (x.Id, x.Score)).ToList()],
            limit);

        var bySku = titleRaw.Concat(descRaw)
            .GroupBy(x => x.Id, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(g => g.Key, g => g.First(), StringComparer.OrdinalIgnoreCase);

        var hits = merged
            .Select(m =>
            {
                if (!bySku.TryGetValue(m.Id, out var scored))
                    return new CatalogBrainHit(m.Id, string.Empty, m.Score);
                return new CatalogBrainHit(
                    scored.Id,
                    scored.Meaning,
                    m.Score,
                    scored.Title,
                    scored.Price,
                    scored.IsActive,
                    scored.ImageUrl,
                    scored.Brand,
                    scored.Seller,
                    Stock: scored.Stock,
                    ProductId: scored.ProductId,
                    SkuId: scored.SkuId,
                    SellerId: scored.SellerId,
                    OptionPairs: scored.OptionPairs);
            })
            .ToList();

        _logger.LogInformation(
            "Qdrant products vector search done. TenantId={TenantId} QueryDims={Dims} TitleHits={TitleHits} DescHits={DescHits} MergedHits={Hits}",
            tenantId,
            query.Length,
            titleRaw.Count,
            descRaw.Count,
            hits.Count);

        return hits;
    }

    public Task UpsertAsync(string tenantId, string sku, string meaning, CancellationToken cancellationToken = default)
        => UpsertAsync(
            new CatalogProductDocument(tenantId, sku, meaning, meaning, null, 0, true, null),
            cancellationToken);

    public async Task UpsertAsync(CatalogProductDocument document, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(document.TenantId) || string.IsNullOrWhiteSpace(document.Sku))
            throw new ArgumentException("tenantId and sku are required.");

        await EnsureCollectionAsync(cancellationToken);

        var title = string.IsNullOrWhiteSpace(document.Title) ? document.Sku : document.Title;
        var description = string.IsNullOrWhiteSpace(document.Description) ? title : document.Description;
        var pointId = PointId(document.TenantId, document.Sku);

        _logger.LogInformation(
            "Qdrant products upsert. TenantId={TenantId} Sku={Sku} PointId={PointId} Title={Title} Description={Description} Category={Category} Brand={Brand} Seller={Seller} Price={Price} IsActive={IsActive} OptionPairs={OptionPairs} ImageUrl={ImageUrl}",
            document.TenantId,
            document.Sku,
            pointId,
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

        // Image vectors live on separate doc_type=image points (catalog-image-vector queue).
        var named = new NamedVectors();
        named.Vectors[VectorTitle] = titleVec;
        named.Vectors[VectorDescription] = descVec;

        var optionKeys = document.OptionKeys?.Where(x => !string.IsNullOrWhiteSpace(x)).Distinct(StringComparer.OrdinalIgnoreCase).ToArray()
            ?? Array.Empty<string>();
        var optionPairs = document.OptionPairs?.Where(x => !string.IsNullOrWhiteSpace(x)).Distinct(StringComparer.OrdinalIgnoreCase).ToArray()
            ?? Array.Empty<string>();

        var payload = new Dictionary<string, Value>
        {
            ["tenant_id"] = document.TenantId,
            ["sku"] = document.Sku,
            ["product_id"] = document.ProductId ?? string.Empty,
            ["sku_id"] = document.SkuId ?? document.Sku,
            ["seller_id"] = document.SellerId ?? string.Empty,
            ["title"] = title,
            ["description"] = description,
            ["meaning"] = description,
            ["category"] = document.Category ?? string.Empty,
            ["brand"] = document.Brand ?? string.Empty,
            ["seller"] = document.Seller ?? string.Empty,
            ["price"] = (double)document.Price,
            ["stock"] = document.Stock,
            ["is_active"] = document.IsActive ? "true" : "false",
            ["image_url"] = document.ImageUrl ?? string.Empty,
            ["image_status"] = CatalogImageStatuses.None,
            ["doc_type"] = CatalogDocTypes.Sku,
            ["is_current"] = "true",
            ["option_keys"] = ToListValue(optionKeys),
            ["option_pairs"] = ToListValue(optionPairs)
        };

        _logger.LogInformation(
            "Qdrant products upsert vectors. TenantId={TenantId} Sku={Sku} TitleDims={TitleDims} DescriptionDims={DescriptionDims} DocType={DocType} ImageVectorOmitted=true PayloadKeys={PayloadKeys}",
            document.TenantId,
            document.Sku,
            titleVec.Length,
            descVec.Length,
            CatalogDocTypes.Sku,
            string.Join(',', payload.Keys));

        await _client.UpsertAsync(CollectionName, [new PointStruct
        {
            Id = pointId,
            Vectors = new global::Qdrant.Client.Grpc.Vectors { Vectors_ = named },
            Payload = { payload }
        }], cancellationToken: cancellationToken);

        _logger.LogInformation(
            "Qdrant products upsert done. TenantId={TenantId} Sku={Sku} PointId={PointId}",
            document.TenantId,
            document.Sku,
            pointId);
    }

    public async Task UpsertImageDocumentAsync(
        CatalogImageDocument document,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(document.TenantId) || string.IsNullOrWhiteSpace(document.ImageUrl))
            throw new ArgumentException("tenantId and imageUrl are required.");
        if (document.ImageVector is not { Count: > 0 })
            throw new ArgumentException("imageVector is required.");

        await EnsureCollectionAsync(cancellationToken);

        var normalizedUrl = CatalogImageUrl.Normalize(document.ImageUrl) ?? document.ImageUrl.Trim();
        var sku = string.IsNullOrWhiteSpace(document.Sku) ? normalizedUrl : document.Sku.Trim();
        var title = string.IsNullOrWhiteSpace(document.Title) ? sku : document.Title;
        var pointId = ImagePointId(document.TenantId, normalizedUrl);
        var vector = document.ImageVector as float[] ?? document.ImageVector.ToArray();

        var named = new NamedVectors();
        named.Vectors[VectorImage] = vector;

        var payload = new Dictionary<string, Value>
        {
            ["tenant_id"] = document.TenantId,
            ["sku"] = sku,
            ["product_id"] = document.ProductId ?? string.Empty,
            ["sku_id"] = document.SkuId ?? sku,
            ["seller_id"] = document.SellerId ?? string.Empty,
            ["title"] = title,
            ["description"] = title,
            ["meaning"] = title,
            ["category"] = string.Empty,
            ["brand"] = document.Brand ?? string.Empty,
            ["seller"] = document.Seller ?? string.Empty,
            ["price"] = (double)document.Price,
            ["stock"] = document.Stock,
            ["is_active"] = document.IsActive ? "true" : "false",
            ["image_url"] = normalizedUrl,
            ["image_status"] = CatalogImageStatuses.Indexed,
            ["doc_type"] = CatalogDocTypes.Image,
            ["is_current"] = "true",
            ["option_keys"] = ToListValue(Array.Empty<string>()),
            ["option_pairs"] = ToListValue(Array.Empty<string>())
        };

        _logger.LogInformation(
            "Qdrant products image-doc upsert. TenantId={TenantId} ProductId={ProductId} Sku={Sku} PointId={PointId} ImageDims={ImageDims} ImageUrl={ImageUrl}",
            document.TenantId,
            document.ProductId,
            sku,
            pointId,
            vector.Length,
            Truncate(normalizedUrl, 200));

        await _client.UpsertAsync(CollectionName, [new PointStruct
        {
            Id = pointId,
            Vectors = new global::Qdrant.Client.Grpc.Vectors { Vectors_ = named },
            Payload = { payload }
        }], cancellationToken: cancellationToken);

        _logger.LogInformation(
            "Qdrant products image-doc upsert done. TenantId={TenantId} PointId={PointId}",
            document.TenantId,
            pointId);
    }

    private static Filter TenantActiveFilter(
        string tenantId,
        string docType,
        IReadOnlyList<string>? optionPairs = null)
    {
        var filter = new Filter
        {
            Must =
            {
                MatchKeyword("tenant_id", tenantId),
                MatchKeyword("doc_type", docType)
            }
        };

        if (optionPairs is { Count: > 0 })
        {
            foreach (var pair in optionPairs)
                filter.Must.Add(MatchKeyword("option_pairs", pair));
        }

        return filter;
    }

    private static Value ToListValue(IReadOnlyList<string> values)
    {
        var list = new ListValue();
        foreach (var value in values)
            list.Values.Add(value);
        return new Value { ListValue = list };
    }

    private static (string Id, float Score, string Meaning, string? Title, decimal? Price, bool? IsActive, string? ImageUrl, string? Brand, string? Seller, string? ProductId, string? SkuId, string? SellerId, int? Stock, IReadOnlyList<string>? OptionPairs) ToScored(ScoredPoint p)
    {
        p.Payload.TryGetValue("sku", out var sku);
        p.Payload.TryGetValue("product_id", out var productId);
        p.Payload.TryGetValue("sku_id", out var skuId);
        p.Payload.TryGetValue("seller_id", out var sellerId);
        p.Payload.TryGetValue("meaning", out var meaning);
        p.Payload.TryGetValue("description", out var description);
        p.Payload.TryGetValue("title", out var title);
        p.Payload.TryGetValue("image_url", out var imageUrl);
        p.Payload.TryGetValue("brand", out var brand);
        p.Payload.TryGetValue("seller", out var seller);
        p.Payload.TryGetValue("is_active", out var isActive);
        decimal? price = null;
        if (p.Payload.TryGetValue("price", out var priceVal))
        {
            if (priceVal.KindCase == Value.KindOneofCase.DoubleValue)
                price = (decimal)priceVal.DoubleValue;
            else if (priceVal.KindCase == Value.KindOneofCase.IntegerValue)
                price = priceVal.IntegerValue;
            else if (priceVal.KindCase == Value.KindOneofCase.StringValue
                     && decimal.TryParse(priceVal.StringValue, out var parsed))
                price = parsed;
        }

        int? stock = null;
        if (p.Payload.TryGetValue("stock", out var stockVal))
        {
            if (stockVal.KindCase == Value.KindOneofCase.IntegerValue)
                stock = (int)stockVal.IntegerValue;
            else if (stockVal.KindCase == Value.KindOneofCase.DoubleValue)
                stock = (int)stockVal.DoubleValue;
            else if (stockVal.KindCase == Value.KindOneofCase.StringValue
                     && int.TryParse(stockVal.StringValue, out var parsedStock))
                stock = parsedStock;
        }

        bool? active = null;
        if (isActive is not null)
        {
            if (isActive.KindCase == Value.KindOneofCase.BoolValue)
                active = isActive.BoolValue;
            else if (isActive.KindCase == Value.KindOneofCase.StringValue)
                active = string.Equals(isActive.StringValue, "true", StringComparison.OrdinalIgnoreCase);
        }

        IReadOnlyList<string>? optionPairs = null;
        if (p.Payload.TryGetValue("option_pairs", out var pairsVal))
            optionPairs = ReadStringList(pairsVal);

        var skuValue = sku?.StringValue ?? string.Empty;
        return (
            skuValue,
            p.Score,
            meaning?.StringValue ?? description?.StringValue ?? title?.StringValue ?? string.Empty,
            string.IsNullOrWhiteSpace(title?.StringValue) ? null : title!.StringValue,
            price,
            active,
            string.IsNullOrWhiteSpace(imageUrl?.StringValue) ? null : imageUrl!.StringValue,
            string.IsNullOrWhiteSpace(brand?.StringValue) ? null : brand!.StringValue,
            string.IsNullOrWhiteSpace(seller?.StringValue) ? null : seller!.StringValue,
            string.IsNullOrWhiteSpace(productId?.StringValue) ? null : productId!.StringValue,
            string.IsNullOrWhiteSpace(skuId?.StringValue) ? skuValue : skuId!.StringValue,
            string.IsNullOrWhiteSpace(sellerId?.StringValue) ? null : sellerId!.StringValue,
            stock,
            optionPairs);
    }

    private static IReadOnlyList<string>? ReadStringList(Value value)
    {
        if (value.KindCase == Value.KindOneofCase.ListValue)
        {
            var list = value.ListValue.Values
                .Where(v => v.KindCase == Value.KindOneofCase.StringValue && !string.IsNullOrWhiteSpace(v.StringValue))
                .Select(v => v.StringValue)
                .ToArray();
            return list.Length == 0 ? null : list;
        }

        if (value.KindCase == Value.KindOneofCase.StringValue && !string.IsNullOrWhiteSpace(value.StringValue))
            return [value.StringValue];

        return null;
    }

    private static CatalogBrainHit ToHit(ScoredPoint hit)
    {
        var scored = ToScored(hit);
        return new CatalogBrainHit(
            scored.Id,
            scored.Meaning,
            scored.Score,
            scored.Title,
            scored.Price,
            scored.IsActive,
            scored.ImageUrl,
            scored.Brand,
            scored.Seller,
            Stock: scored.Stock,
            ProductId: scored.ProductId,
            SkuId: scored.SkuId,
            SellerId: scored.SellerId,
            OptionPairs: scored.OptionPairs);
    }

    private static ulong PointId(string tenantId, string sku)
    {
        var bytes = System.Security.Cryptography.SHA256.HashData(
            System.Text.Encoding.UTF8.GetBytes($"{tenantId}|{sku}"));
        return BitConverter.ToUInt64(bytes, 0);
    }

    private static ulong ImagePointId(string tenantId, string normalizedImageUrl)
    {
        var bytes = System.Security.Cryptography.SHA256.HashData(
            System.Text.Encoding.UTF8.GetBytes($"{tenantId}|img|{normalizedImageUrl}"));
        return BitConverter.ToUInt64(bytes, 0);
    }

    private static string Truncate(string? value, int max = 240)
    {
        if (string.IsNullOrEmpty(value))
            return string.Empty;
        return value.Length <= max ? value : value[..max] + "…";
    }
}

public sealed class QdrantProductsBootstrapper(QdrantProductsIndex index) : IVectorIndexBootstrapper
{
    public Task EnsureAsync(CancellationToken cancellationToken = default)
        => index.EnsureCollectionAsync(cancellationToken);
}
