using System.Text;
using System.Text.Json;
using Commerce.Application.Abstracts;

namespace Commerce.Application.Features.DataIngestion;

public static class GravityCatalogBrainMapper
{
    public sealed record SkuBrainItem(
        string Sku,
        string Title,
        string Description,
        string? Category,
        decimal Price,
        bool IsActive,
        string? ImageUrl,
        CatalogSkuSnapshot Snapshot,
        string? Brand = null,
        string? Seller = null,
        string? ProductId = null,
        string? SkuId = null,
        string? SellerId = null,
        IReadOnlyList<string>? OptionKeys = null,
        IReadOnlyList<string>? OptionPairs = null)
    {
        public string Meaning => Description;
    }

    /// <summary>
    /// Mapped SKUs plus the capped canonical product image gallery used for vector indexing.
    /// </summary>
    public sealed record ProductBrainMap(
        IReadOnlyList<SkuBrainItem> Skus,
        IReadOnlyList<string> CanonicalImageUrls);

    public static ProductBrainMap MapProduct(string? payloadJson, int maxImagesPerProduct = 7)
    {
        if (string.IsNullOrWhiteSpace(payloadJson))
            return new ProductBrainMap([], []);

        var maxImages = Math.Clamp(maxImagesPerProduct, 1, 20);

        try
        {
            using var doc = JsonDocument.Parse(payloadJson);
            var root = doc.RootElement;
            var productId = ReadString(root, "id")
                ?? ReadString(root, "productId")
                ?? ReadString(root, "uniqueId");
            var name = ReadString(root, "name") ?? string.Empty;
            var brand = ReadString(root, "brandName");
            var seller = ReadString(root, "sellerName")
                ?? ReadString(root, "principalSellerName")
                ?? ReadSellerNestedName(root);
            var sellerId = ReadString(root, "sellerId")
                ?? ReadString(root, "principalSellerId")
                ?? ReadSellerNestedId(root);
            var category = ReadString(root, "categoryPath");
            var keywords = ReadString(root, "keywords") ?? ReadString(root, "description");
            var productStatus = ReadString(root, "productStatusName");
            var isActive = IsActive(productStatus);
            var productOptions = ReadOptionsMap(root);

            if (!root.TryGetProperty("skus", out var skus) || skus.ValueKind != JsonValueKind.Array)
                return new ProductBrainMap([], []);

            var skuElements = skus.EnumerateArray().ToList();
            var canonicalImages = BuildCanonicalImageGallery(root, skuElements, maxImages);
            var galleryFirst = canonicalImages.FirstOrDefault();

            var items = new List<SkuBrainItem>();
            foreach (var skuEl in skuElements)
            {
                var skuId = ReadString(skuEl, "skuId")
                    ?? ReadString(skuEl, "id");
                var skuCode = ReadString(skuEl, "sku")
                    ?? ReadString(skuEl, "skuExternalId");
                // Document key / cart line: prefer Gravity skuId.
                var sku = skuId ?? skuCode;
                if (string.IsNullOrWhiteSpace(sku))
                    continue;

                var merged = MergeOptions(productOptions, ReadOptionsMap(skuEl));
                var optionKeys = merged.Keys.OrderBy(k => k, StringComparer.Ordinal).ToList();
                var optionPairs = merged
                    .OrderBy(kv => kv.Key, StringComparer.Ordinal)
                    .Select(kv => CatalogOptionNormalizer.FormatPair(kv.Key, kv.Value))
                    .ToList();

                var description = BuildMeaning(name, brand, category, keywords, merged, skuEl);
                var stock = (int)(ReadDecimal(skuEl, "totalStock") ?? ReadDecimal(root, "totalStock") ?? 0);
                var basePrice = ReadDecimal(skuEl, "basePrice") ?? 0;
                var special = ReadDecimal(skuEl, "specialPrice");
                var price = special is > 0 ? special.Value : basePrice;
                var cost = ReadDecimal(skuEl, "costPrice") ?? 0;
                var skuImages = CollectImageEntries(skuEl);
                var imageUrl = skuImages.Count > 0 ? skuImages[0].Url : galleryFirst;
                var lineSellerId = ReadString(skuEl, "sellerId") ?? sellerId;
                var skuActive = isActive && IsSkuActive(skuEl);
                items.Add(new SkuBrainItem(
                    sku,
                    string.IsNullOrWhiteSpace(name) ? sku : name,
                    description,
                    category,
                    price,
                    skuActive,
                    imageUrl,
                    new CatalogSkuSnapshot(sku, stock, price, cost, skuActive ? "active" : "paused"),
                    brand,
                    seller,
                    productId,
                    skuId ?? sku,
                    lineSellerId,
                    optionKeys,
                    optionPairs));
            }

            return new ProductBrainMap(items, canonicalImages);
        }
        catch (JsonException)
        {
            return new ProductBrainMap([], []);
        }
    }

    /// <summary>
    /// Product gallery first (stable order), then SKU-only URLs not already present (by image id or URL).
    /// Capped to <paramref name="maxImages"/>.
    /// </summary>
    private static IReadOnlyList<string> BuildCanonicalImageGallery(
        JsonElement root,
        IReadOnlyList<JsonElement> skuElements,
        int maxImages)
    {
        var urls = new List<string>();
        var seenIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var seenUrls = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        void TryAdd(ImageEntry entry)
        {
            if (urls.Count >= maxImages)
                return;
            if (!string.IsNullOrWhiteSpace(entry.Id) && !seenIds.Add(entry.Id))
                return;
            if (!seenUrls.Add(entry.Url))
                return;
            urls.Add(entry.Url);
        }

        foreach (var entry in CollectImageEntries(root))
            TryAdd(entry);

        foreach (var skuEl in skuElements)
        {
            foreach (var entry in CollectImageEntries(skuEl))
                TryAdd(entry);
        }

        return urls;
    }

    private static string BuildMeaning(
        string? name,
        string? brand,
        string? category,
        string? keywords,
        IReadOnlyDictionary<string, string> options,
        JsonElement sku)
    {
        var text = new StringBuilder();
        if (!string.IsNullOrWhiteSpace(name))
            text.Append(name);
        if (!string.IsNullOrWhiteSpace(brand))
        {
            if (text.Length > 0) text.Append(". ");
            text.Append(brand);
        }

        if (!string.IsNullOrWhiteSpace(category))
        {
            if (text.Length > 0) text.Append(". ");
            text.Append(category);
        }

        if (options.Count > 0)
        {
            if (text.Length > 0) text.Append(". ");
            text.Append(string.Join(", ", options.Select(kv => $"{kv.Key} {kv.Value}")));
        }
        else
        {
            var legacy = ReadSkuOptionLabels(sku);
            if (legacy.Count > 0)
            {
                if (text.Length > 0) text.Append(". ");
                text.Append(string.Join(", ", legacy));
            }
        }

        if (!string.IsNullOrWhiteSpace(keywords))
        {
            if (text.Length > 0) text.Append(". ");
            text.Append(keywords);
        }

        if (text.Length == 0)
            text.Append(ReadString(sku, "sku") ?? "product");

        text.Append('.');
        return text.ToString();
    }

    private static Dictionary<string, string> ReadOptionsMap(JsonElement item)
    {
        var map = new Dictionary<string, string>(StringComparer.Ordinal);
        if (item.TryGetProperty("options", out var options) && options.ValueKind == JsonValueKind.Array)
        {
            foreach (var opt in options.EnumerateArray())
            {
                var key = ReadString(opt, "key") ?? ReadString(opt, "name") ?? ReadString(opt, "optionName");
                var value = ReadString(opt, "value") ?? ReadString(opt, "optionValue");
                if (!CatalogOptionNormalizer.TryNormalize(key, value, out var canonicalKey, out var canonicalValue, out _))
                    continue;
                map[canonicalKey] = canonicalValue;
            }
        }

        if (CatalogOptionNormalizer.TryNormalize("COLOR", ReadString(item, "color"), out var colorKey, out var colorValue, out _))
            map[colorKey] = colorValue;
        if (CatalogOptionNormalizer.TryNormalize("SIZE", ReadString(item, "size"), out var sizeKey, out var sizeValue, out _))
            map[sizeKey] = sizeValue;

        return map;
    }

    private static Dictionary<string, string> MergeOptions(
        IReadOnlyDictionary<string, string> product,
        IReadOnlyDictionary<string, string> sku)
    {
        var merged = new Dictionary<string, string>(product, StringComparer.Ordinal);
        foreach (var (key, value) in sku)
            merged[key] = value;
        return merged;
    }

    private static List<string> ReadSkuOptionLabels(JsonElement sku)
    {
        var parts = new List<string>();
        if (sku.TryGetProperty("options", out var options) && options.ValueKind == JsonValueKind.Array)
        {
            foreach (var opt in options.EnumerateArray())
            {
                var label = ReadString(opt, "name") ?? ReadString(opt, "optionName");
                var value = ReadString(opt, "value") ?? ReadString(opt, "optionValue");
                if (!string.IsNullOrWhiteSpace(label) && !string.IsNullOrWhiteSpace(value))
                    parts.Add($"{label} {value}");
                else if (!string.IsNullOrWhiteSpace(value))
                    parts.Add(value);
            }
        }

        return parts;
    }

    private readonly record struct ImageEntry(string? Id, string Url);

    /// <summary>
    /// Collect unique image entries from <c>imageList</c> / <c>imageUrl</c> / <c>image</c> on this element only
    /// (does not walk nested SKUs). Prefers Gravity/ML image <c>id</c> for dedupe when present.
    /// </summary>
    private static List<ImageEntry> CollectImageEntries(JsonElement item)
    {
        var entries = new List<ImageEntry>();
        var seenIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var seenUrls = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        void Add(string? id, string? rawUrl)
        {
            var normalized = CatalogImageUrl.Normalize(rawUrl) ?? rawUrl?.Trim();
            if (string.IsNullOrWhiteSpace(normalized))
                return;
            if (!string.IsNullOrWhiteSpace(id) && !seenIds.Add(id.Trim()))
                return;
            if (!seenUrls.Add(normalized))
                return;
            entries.Add(new ImageEntry(string.IsNullOrWhiteSpace(id) ? null : id.Trim(), normalized));
        }

        if (item.TryGetProperty("imageList", out var images) && images.ValueKind == JsonValueKind.Array)
        {
            foreach (var image in images.EnumerateArray())
                Add(ReadString(image, "id"), ReadString(image, "link"));
        }

        Add(null, ReadString(item, "imageUrl"));
        Add(null, ReadString(item, "image"));
        return entries;
    }

    private static string? ReadSellerNestedName(JsonElement root)
    {
        if (root.TryGetProperty("seller", out var seller) && seller.ValueKind == JsonValueKind.Object)
            return ReadString(seller, "name");
        return null;
    }

    private static string? ReadSellerNestedId(JsonElement root)
    {
        if (root.TryGetProperty("seller", out var seller) && seller.ValueKind == JsonValueKind.Object)
            return ReadString(seller, "id") ?? ReadString(seller, "sellerId");
        return null;
    }

    private static bool IsActive(string? productStatus)
    {
        if (string.IsNullOrWhiteSpace(productStatus))
            return true;
        return productStatus.Contains("active", StringComparison.OrdinalIgnoreCase)
            || productStatus.Contains("publicado", StringComparison.OrdinalIgnoreCase)
            || productStatus.Equals("Published", StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsSkuActive(JsonElement sku)
    {
        if (sku.TryGetProperty("isActive", out var flag)
            && (flag.ValueKind is JsonValueKind.True or JsonValueKind.False))
        {
            return flag.GetBoolean();
        }

        var status = ReadString(sku, "skuStatusName");
        if (string.IsNullOrWhiteSpace(status))
            return true;
        return IsActive(status);
    }

    private static string? ReadString(JsonElement el, string name)
    {
        if (!el.TryGetProperty(name, out var prop))
            return null;
        return prop.ValueKind == JsonValueKind.String ? prop.GetString() : prop.ToString();
    }

    private static decimal? ReadDecimal(JsonElement el, string name)
    {
        if (!el.TryGetProperty(name, out var prop))
            return null;
        return prop.ValueKind switch
        {
            JsonValueKind.Number => prop.TryGetDecimal(out var d) ? d : null,
            JsonValueKind.String when decimal.TryParse(prop.GetString(), out var d) => d,
            _ => null
        };
    }
}
