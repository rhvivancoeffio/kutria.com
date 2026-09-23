using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Commerce.Application.Abstracts;
using Commerce.Application.Features.DataIngestion;
using Microsoft.Extensions.Logging;

namespace Commerce.Infrastructure.Integrations;

public sealed class GravityStoreDataClient(
    IHttpClientFactory httpClientFactory,
    ILogger<GravityStoreDataClient> logger) : IGravityStoreDataClient
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    public async Task<GravityProductPage> ListProductsAsync(
        GravityStoreCredentials credentials,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 100);
        // Keep SKUs so list rows can show the first product image.
        var path = $"Products?Page={page}&PageSize={pageSize}&ExcludeSkus=false";
        using var doc = await SendAsync(credentials, HttpMethod.Get, path, body: null, cancellationToken);
        var root = doc.RootElement;
        var results = new List<GravityProductListItem>();
        if (root.TryGetProperty("results", out var arr) && arr.ValueKind == JsonValueKind.Array)
        {
            foreach (var item in arr.EnumerateArray())
                results.Add(MapProductListItem(item));
        }
        else
        {
            logger.LogWarning(
                "Gravity products response has no results[]. RootKind={RootKind} Preview={Preview}",
                root.ValueKind,
                Truncate(root.GetRawText()));
        }

        return new GravityProductPage(
            root.TryGetProperty("currentPage", out var cp) && cp.TryGetInt32(out var current) ? current : page,
            root.TryGetProperty("pageCount", out var pc) && pc.TryGetInt32(out var pages) ? pages : 1,
            root.TryGetProperty("pageSize", out var ps) && ps.TryGetInt32(out var size) ? size : pageSize,
            root.TryGetProperty("rowCount", out var rc) && rc.TryGetInt32(out var rows) ? rows : results.Count,
            results);
    }

    public async Task<GravityProductDetail?> GetProductByIdAsync(
        GravityStoreCredentials credentials,
        string productId,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(productId))
            return null;

        try
        {
            using var doc = await SendAsync(
                credentials,
                HttpMethod.Get,
                $"Products/{Uri.EscapeDataString(productId.Trim())}",
                body: null,
                cancellationToken);
            var mapped = MapProductDetail(doc.RootElement);
            return string.IsNullOrWhiteSpace(mapped.ProductId) ? null : mapped;
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("(404)", StringComparison.Ordinal))
        {
            return null;
        }
    }

    public async Task<GravityOrderPage> ListOrdersAsync(
        GravityStoreCredentials credentials,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 100);
        var path = $"Orders?Page={page}&PageSize={pageSize}&OnlySaleOrders=true";
        using var doc = await SendAsync(credentials, HttpMethod.Get, path, body: null, cancellationToken);
        var root = doc.RootElement;
        var results = new List<GravityOrderItem>();

        if (root.ValueKind == JsonValueKind.Array)
        {
            foreach (var item in root.EnumerateArray())
                results.Add(MapOrder(item));
        }
        else if (root.TryGetProperty("results", out var arr) && arr.ValueKind == JsonValueKind.Array)
        {
            foreach (var item in arr.EnumerateArray())
                results.Add(MapOrder(item));
        }
        else
        {
            logger.LogWarning(
                "Gravity orders response has unexpected shape. RootKind={RootKind} Preview={Preview}",
                root.ValueKind,
                Truncate(root.GetRawText()));
        }

        return new GravityOrderPage(page, pageSize, results.Count >= pageSize, results);
    }

    public async Task<GravityOrderItem?> GetOrderByIdAsync(
        GravityStoreCredentials credentials,
        string orderId,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(orderId))
            return null;

        using var doc = await SendAsync(credentials, HttpMethod.Get, $"Orders/{Uri.EscapeDataString(orderId)}", body: null, cancellationToken);
        return MapOrder(doc.RootElement);
    }

    public async Task<IReadOnlyList<GravityBrandItem>> ListBrandsAsync(
        GravityStoreCredentials credentials,
        string? name = null,
        int page = 1,
        int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 100);
        var qs = $"Page={page}&PageSize={pageSize}";
        if (!string.IsNullOrWhiteSpace(name))
            qs += $"&Name={Uri.EscapeDataString(name.Trim())}";

        using var doc = await SendAsync(credentials, HttpMethod.Get, $"Brands?{qs}", body: null, cancellationToken);
        return MapBrandList(doc.RootElement);
    }

    public async Task<GravityBrandItem?> GetBrandAsync(
        GravityStoreCredentials credentials,
        string brandId,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(brandId))
            return null;

        try
        {
            using var doc = await SendAsync(
                credentials,
                HttpMethod.Get,
                $"Brands/{Uri.EscapeDataString(brandId.Trim())}",
                body: null,
                cancellationToken);
            var mapped = MapBrand(doc.RootElement);
            return string.IsNullOrWhiteSpace(mapped.BrandId) ? null : mapped;
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("(404)", StringComparison.Ordinal))
        {
            return null;
        }
    }

    public async Task<IReadOnlyList<GravityBrandItem>> AutocompleteBrandsAsync(
        GravityStoreCredentials credentials,
        string name,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(name))
            return [];

        var path = $"Brands/autocomplete?Name={Uri.EscapeDataString(name.Trim())}";
        using var doc = await SendAsync(credentials, HttpMethod.Get, path, body: null, cancellationToken);
        var root = doc.RootElement;
        var results = new List<GravityBrandItem>();
        if (root.ValueKind == JsonValueKind.Array)
        {
            foreach (var item in root.EnumerateArray())
            {
                var id = ReadString(item, "id") ?? ReadString(item, "brandId") ?? string.Empty;
                if (string.IsNullOrWhiteSpace(id)) continue;
                results.Add(new GravityBrandItem(id, ReadString(item, "name")));
            }
        }

        return results;
    }

    public async Task<GravityBrandItem> CreateBrandAsync(
        GravityStoreCredentials credentials,
        GravityBrandCreateRequest request,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
            throw new ArgumentException("Brand name is required.", nameof(request));

        var body = new
        {
            name = request.Name.Trim(),
            description = request.Description,
            isActive = request.IsActive,
            imageUrl = request.ImageUrl
        };
        var (payload, location) = await SendRawAsync(
            credentials,
            HttpMethod.Post,
            "Brands",
            body,
            cancellationToken);
        var mapped = MapBrandWrite(payload, location, request.Name.Trim());
        if (string.IsNullOrWhiteSpace(mapped.BrandId))
        {
            throw new InvalidOperationException(
                "Gravity POST Brands did not return brandId (body or Location).");
        }

        return mapped;
    }

    public async Task<GravityBrandItem> UpdateBrandAsync(
        GravityStoreCredentials credentials,
        string brandId,
        GravityBrandCreateRequest request,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(brandId))
            throw new ArgumentException("brandId is required.", nameof(brandId));

        var body = new
        {
            name = request.Name,
            description = request.Description,
            isActive = request.IsActive,
            imageUrl = request.ImageUrl
        };
        using var doc = await SendAsync(
            credentials,
            HttpMethod.Put,
            $"Brands/{Uri.EscapeDataString(brandId)}",
            body,
            cancellationToken);
        return MapBrand(doc.RootElement);
    }

    public async Task DeleteBrandAsync(
        GravityStoreCredentials credentials,
        string brandId,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(brandId))
            throw new ArgumentException("brandId is required.", nameof(brandId));

        using var _ = await SendAsync(
            credentials,
            HttpMethod.Delete,
            $"Brands/{Uri.EscapeDataString(brandId)}",
            body: null,
            cancellationToken);
    }

    public async Task<IReadOnlyList<GravityEntityAttributeItem>> ListEntityAttributesAsync(
        GravityStoreCredentials credentials,
        string? name = null,
        int page = 1,
        int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 100);
        var qs = $"Page={page}&PageSize={pageSize}";
        if (!string.IsNullOrWhiteSpace(name))
            qs += $"&Name={Uri.EscapeDataString(name.Trim())}";

        using var doc = await SendAsync(credentials, HttpMethod.Get, $"EntityAttributes?{qs}", body: null, cancellationToken);
        return MapEntityAttributeList(doc.RootElement);
    }

    public async Task<GravityEntityAttributeItem?> GetEntityAttributeAsync(
        GravityStoreCredentials credentials,
        string entityAttributeId,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(entityAttributeId))
            return null;

        try
        {
            using var doc = await SendAsync(
                credentials,
                HttpMethod.Get,
                $"EntityAttributes/{Uri.EscapeDataString(entityAttributeId.Trim())}",
                body: null,
                cancellationToken);
            var mapped = MapEntityAttribute(doc.RootElement);
            return string.IsNullOrWhiteSpace(mapped.EntityAttributeId) ? null : mapped;
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("(404)", StringComparison.Ordinal))
        {
            return null;
        }
    }

    public async Task<GravityEntityAttributeItem> CreateEntityAttributeAsync(
        GravityStoreCredentials credentials,
        GravityEntityAttributeWriteRequest request,
        CancellationToken cancellationToken = default)
    {
        using var doc = await SendAsync(
            credentials,
            HttpMethod.Post,
            "EntityAttributes",
            BuildEntityAttributeBody(request),
            cancellationToken);
        return MapEntityAttribute(doc.RootElement);
    }

    public async Task<GravityEntityAttributeItem> UpdateEntityAttributeAsync(
        GravityStoreCredentials credentials,
        string entityAttributeId,
        GravityEntityAttributeWriteRequest request,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(entityAttributeId))
            throw new ArgumentException("entityAttributeId is required.", nameof(entityAttributeId));

        using var doc = await SendAsync(
            credentials,
            HttpMethod.Put,
            $"EntityAttributes/{Uri.EscapeDataString(entityAttributeId.Trim())}",
            BuildEntityAttributeBody(request),
            cancellationToken);
        return MapEntityAttribute(doc.RootElement);
    }

    public async Task DeleteEntityAttributeAsync(
        GravityStoreCredentials credentials,
        string entityAttributeId,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(entityAttributeId))
            throw new ArgumentException("entityAttributeId is required.", nameof(entityAttributeId));

        using var _ = await SendAsync(
            credentials,
            HttpMethod.Delete,
            $"EntityAttributes/{Uri.EscapeDataString(entityAttributeId.Trim())}",
            body: null,
            cancellationToken);
    }

    public async Task<IReadOnlyList<GravityCategoryItem>> ListCategoriesAsync(
        GravityStoreCredentials credentials,
        string? name = null,
        CancellationToken cancellationToken = default)
    {
        // Product create validates against CategoryCacheService (GET Categories/all).
        // Use that tree so list/picker and CreateProduct agree; newly created categories
        // may be missing until Gravity refreshes that cache.
        using var doc = await SendAsync(
            credentials,
            HttpMethod.Get,
            "Categories/all",
            body: null,
            cancellationToken);
        var flat = FlattenCategoryTree(doc.RootElement);
        if (string.IsNullOrWhiteSpace(name))
            return flat;

        var q = name.Trim();
        return flat
            .Where(c => !string.IsNullOrWhiteSpace(c.Name)
                        && c.Name.Contains(q, StringComparison.OrdinalIgnoreCase))
            .ToList();
    }

    public async Task<GravityCategoryItem?> GetCategoryAsync(
        GravityStoreCredentials credentials,
        string categoryId,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(categoryId))
            return null;

        try
        {
            using var doc = await SendAsync(
                credentials,
                HttpMethod.Get,
                $"Categories/{Uri.EscapeDataString(categoryId.Trim())}",
                body: null,
                cancellationToken);
            var mapped = MapCategory(doc.RootElement);
            return string.IsNullOrWhiteSpace(mapped.CategoryId) ? null : mapped;
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("(404)", StringComparison.Ordinal))
        {
            return null;
        }
    }

    public async Task<GravityCategoryItem> CreateCategoryAsync(
        GravityStoreCredentials credentials,
        GravityCategoryCreateRequest request,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
            throw new ArgumentException("Category name is required.", nameof(request));

        var slug = string.IsNullOrWhiteSpace(request.Slug)
            ? Slugify(request.Name)
            : request.Slug.Trim();
        var body = new
        {
            name = request.Name.Trim(),
            slug,
            description = request.Description,
            parentCategoryId = request.ParentCategoryId,
            isActive = request.IsActive
        };
        var (payload, location) = await SendRawAsync(
            credentials,
            HttpMethod.Post,
            "Categories",
            body,
            cancellationToken);
        var mapped = MapCategoryWrite(payload, location, request.Name.Trim());
        if (string.IsNullOrWhiteSpace(mapped.CategoryId))
        {
            throw new InvalidOperationException(
                "Gravity POST Categories did not return categoryId (body or Location).");
        }

        return mapped;
    }

    public async Task<GravityCategoryItem> UpdateCategoryAsync(
        GravityStoreCredentials credentials,
        string categoryId,
        GravityCategoryCreateRequest request,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(categoryId))
            throw new ArgumentException("categoryId is required.", nameof(categoryId));

        var slug = string.IsNullOrWhiteSpace(request.Slug)
            ? Slugify(request.Name)
            : request.Slug.Trim();
        var body = new
        {
            name = request.Name,
            slug,
            description = request.Description,
            parentCategoryId = request.ParentCategoryId,
            isActive = request.IsActive
        };
        using var doc = await SendAsync(
            credentials,
            HttpMethod.Put,
            $"Categories/{Uri.EscapeDataString(categoryId)}",
            body,
            cancellationToken);
        return MapCategory(doc.RootElement);
    }

    public async Task DeleteCategoryAsync(
        GravityStoreCredentials credentials,
        string categoryId,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(categoryId))
            throw new ArgumentException("categoryId is required.", nameof(categoryId));

        using var _ = await SendAsync(
            credentials,
            HttpMethod.Delete,
            $"Categories/{Uri.EscapeDataString(categoryId)}",
            body: null,
            cancellationToken);
    }

    public async Task<GravityProductWriteResult> CreateProductAsync(
        GravityStoreCredentials credentials,
        GravityProductCreateRequest request,
        CancellationToken cancellationToken = default)
    {
        var withSeller = await EnsureProductSellerAsync(credentials, request, cancellationToken);
        var body = BuildProductBody(withSeller);
        using var doc = await SendAsync(credentials, HttpMethod.Post, "Products", body, cancellationToken);
        return MapProductWrite(doc.RootElement);
    }

    public async Task<GravityProductWriteResult> UpdateProductAsync(
        GravityStoreCredentials credentials,
        string productId,
        GravityProductCreateRequest request,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(productId))
            throw new ArgumentException("productId is required.", nameof(productId));

        var body = BuildProductBody(request);
        using var doc = await SendAsync(
            credentials,
            HttpMethod.Put,
            $"Products/{Uri.EscapeDataString(productId)}",
            body,
            cancellationToken);
        return MapProductWrite(doc.RootElement);
    }

    public async Task DeleteProductAsync(
        GravityStoreCredentials credentials,
        string productId,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(productId))
            throw new ArgumentException("productId is required.", nameof(productId));

        using var _ = await SendAsync(
            credentials,
            HttpMethod.Delete,
            $"Products/{Uri.EscapeDataString(productId)}",
            body: null,
            cancellationToken);
    }

    public async Task PublishProductAsync(
        GravityStoreCredentials credentials,
        string productId,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(productId))
            throw new ArgumentException("productId is required.", nameof(productId));

        using var _ = await SendAsync(
            credentials,
            HttpMethod.Post,
            $"Products/{Uri.EscapeDataString(productId)}/enable",
            body: null,
            cancellationToken);
    }

    public async Task<GravityCartResult> CreateCartAsync(
        GravityStoreCredentials credentials,
        GravityCartCreateRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (request.Items is null || request.Items.Count == 0)
            throw new ArgumentException("At least one cart item is required.", nameof(request));

        // Concrete wire DTO (matches CartPostModel / CartItemPostModel shape) — avoid anonymous typed as object.
        var body = new GravityCartPostWire(
            request.Email ?? string.Empty,
            request.Country,
            request.PostalCode,
            request.Source,
            request.Items.Select(ToCartItemWire).ToArray());

        var (payload, location) = await SendRawAsync(
            credentials,
            HttpMethod.Post,
            "Carts",
            body,
            cancellationToken);
        var created = MapCartResult(payload, location, fallbackCartId: null);

        // Some Gravity builds return 200 + empty items on POST /Carts even when items were sent;
        // fall back to POST /Carts/{id}/items with the same line items.
        if (created.ItemCount == 0 && !string.IsNullOrWhiteSpace(created.CartId))
        {
            logger.LogWarning(
                "CreateCart returned empty items (cartId={CartId}). Retrying AddCartItems with same lines.",
                created.CartId);
            return await AddCartItemsAsync(
                    credentials,
                    created.CartId,
                    request.Items,
                    request.PostalCode,
                    cancellationToken)
                .ConfigureAwait(false);
        }

        return created;
    }

    public async Task<GravityCartResult> GetCartAsync(
        GravityStoreCredentials credentials,
        string cartId,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(cartId))
            throw new ArgumentException("cartId is required.", nameof(cartId));

        using var doc = await SendAsync(
            credentials,
            HttpMethod.Get,
            $"Carts/{Uri.EscapeDataString(cartId)}",
            body: null,
            cancellationToken);
        return MapCartResult(doc.RootElement.GetRawText(), location: null, cartId);
    }

    public async Task<GravityCartResult> CheckoutCartAsync(
        GravityStoreCredentials credentials,
        string cartId,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(cartId))
            throw new ArgumentException("cartId is required.", nameof(cartId));

        using var doc = await SendAsync(
            credentials,
            HttpMethod.Post,
            $"Carts/{Uri.EscapeDataString(cartId)}/checkout",
            body: null,
            cancellationToken);
        return MapCartResult(doc.RootElement.GetRawText(), location: null, cartId);
    }

    public async Task<GravityCartResult> AddCartItemsAsync(
        GravityStoreCredentials credentials,
        string cartId,
        IReadOnlyList<GravityCartItemInput> items,
        string? postalCode = null,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(cartId))
            throw new ArgumentException("cartId is required.", nameof(cartId));
        if (items is null || items.Count == 0)
            throw new ArgumentException("At least one cart item is required.", nameof(items));

        var body = new GravityAddItemsWire(postalCode, items.Select(ToCartItemWire).ToArray());
        using var doc = await SendAsync(
            credentials,
            HttpMethod.Post,
            $"Carts/{Uri.EscapeDataString(cartId)}/items",
            body,
            cancellationToken);
        return MapCartResult(doc.RootElement.GetRawText(), location: null, cartId);
    }

    public async Task<GravityCartResult> UpdateCartItemQuantitiesAsync(
        GravityStoreCredentials credentials,
        string cartId,
        IReadOnlyList<GravityCartItemInput> items,
        string? postalCode = null,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(cartId))
            throw new ArgumentException("cartId is required.", nameof(cartId));
        if (items is null || items.Count == 0)
            throw new ArgumentException("At least one cart item is required.", nameof(items));

        var body = new GravityAddItemsWire(postalCode, items.Select(ToCartItemWire).ToArray());
        using var doc = await SendAsync(
            credentials,
            HttpMethod.Put,
            $"Carts/{Uri.EscapeDataString(cartId)}/items/qty",
            body,
            cancellationToken);
        return MapCartResult(doc.RootElement.GetRawText(), location: null, cartId);
    }

    public async Task<GravityCartResult> ApplyCartCouponAsync(
        GravityStoreCredentials credentials,
        string cartId,
        string couponCode,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(cartId))
            throw new ArgumentException("cartId is required.", nameof(cartId));
        if (string.IsNullOrWhiteSpace(couponCode))
            throw new ArgumentException("couponCode is required.", nameof(couponCode));

        var body = new { couponCode };
        using var doc = await SendAsync(
            credentials,
            HttpMethod.Put,
            $"Carts/{Uri.EscapeDataString(cartId)}/coupons",
            body,
            cancellationToken);
        return MapCartResult(doc.RootElement.GetRawText(), location: null, cartId);
    }

    public async Task<GravityCartResult> RemoveCartCouponAsync(
        GravityStoreCredentials credentials,
        string cartId,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(cartId))
            throw new ArgumentException("cartId is required.", nameof(cartId));

        using var doc = await SendAsync(
            credentials,
            HttpMethod.Delete,
            $"Carts/{Uri.EscapeDataString(cartId)}/coupons",
            body: null,
            cancellationToken);
        return MapCartResult(doc.RootElement.GetRawText(), location: null, cartId);
    }

    public async Task<GravityCartResult> RemoveCartItemAsync(
        GravityStoreCredentials credentials,
        string cartId,
        string itemId,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(cartId))
            throw new ArgumentException("cartId is required.", nameof(cartId));
        if (string.IsNullOrWhiteSpace(itemId))
            throw new ArgumentException("itemId is required.", nameof(itemId));

        using var doc = await SendAsync(
            credentials,
            HttpMethod.Delete,
            $"Carts/{Uri.EscapeDataString(cartId)}/items/{Uri.EscapeDataString(itemId)}",
            body: null,
            cancellationToken);
        return MapCartResult(doc.RootElement.GetRawText(), location: null, cartId);
    }

    public async Task<GravityDefaultSeller> GetDefaultSellerAsync(
        GravityStoreCredentials credentials,
        CancellationToken cancellationToken = default)
    {
        using var doc = await SendAsync(
            credentials,
            HttpMethod.Get,
            "sellers/default",
            body: null,
            cancellationToken);
        var root = doc.RootElement;
        var sellerId = ReadString(root, "sellerId") ?? ReadString(root, "id");
        if (string.IsNullOrWhiteSpace(sellerId))
        {
            throw new InvalidOperationException(
                "Gravity GET sellers/default did not return sellerId.");
        }

        return new GravityDefaultSeller(sellerId.Trim(), ReadString(root, "name"));
    }

    private static GravityCartItemWire ToCartItemWire(GravityCartItemInput item)
        => new(item.SkuId, item.SellerId, item.Quantity);

    // Wire shapes aligned with Gravity CartPostModel / CartItemPostModel / AddItemToCartPostModel (camelCase JSON).
    private sealed record GravityCartItemWire(
        [property: JsonPropertyName("skuId")] string SkuId,
        [property: JsonPropertyName("sellerId")] string? SellerId,
        [property: JsonPropertyName("quantity")] int Quantity);

    private sealed record GravityCartPostWire(
        [property: JsonPropertyName("email")] string? Email,
        [property: JsonPropertyName("country")] string? Country,
        [property: JsonPropertyName("postalCode")] string? PostalCode,
        [property: JsonPropertyName("source")] string? Source,
        [property: JsonPropertyName("items")] IReadOnlyList<GravityCartItemWire> Items);

    private sealed record GravityAddItemsWire(
        [property: JsonPropertyName("postalCode")] string? PostalCode,
        [property: JsonPropertyName("items")] IReadOnlyList<GravityCartItemWire> Items);

    private async Task<GravityProductCreateRequest> EnsureProductSellerAsync(
        GravityStoreCredentials credentials,
        GravityProductCreateRequest request,
        CancellationToken cancellationToken)
    {
        if (!string.IsNullOrWhiteSpace(request.SellerId))
            return request;

        if (!string.IsNullOrWhiteSpace(credentials.DefaultSellerId))
            return request with { SellerId = credentials.DefaultSellerId.Trim() };

        var seller = await GetDefaultSellerAsync(credentials, cancellationToken);
        logger.LogInformation(
            "Gravity product create: resolved default seller. SellerId={SellerId} Name={Name}",
            seller.SellerId,
            seller.Name ?? "(none)");
        return request with { SellerId = seller.SellerId };
    }

    private static object BuildProductBody(GravityProductCreateRequest request)
    {
        object? images = null;
        if (!string.IsNullOrWhiteSpace(request.ImageUrl))
        {
            images = new[]
            {
                new { link = request.ImageUrl, isPrincipal = true }
            };
        }

        var variations = BuildVariationsWire(request);

        return new
        {
            name = request.Name,
            description = string.IsNullOrWhiteSpace(request.Description) ? request.Name : request.Description,
            brandId = string.IsNullOrWhiteSpace(request.BrandId) ? null : request.BrandId.Trim(),
            brand = new { id = request.BrandId, name = request.BrandName },
            categoryId = string.IsNullOrWhiteSpace(request.CategoryId) ? null : request.CategoryId.Trim(),
            category = new { id = request.CategoryId, path = request.CategoryPath },
            images,
            isActive = request.IsActive,
            showInCatalog = request.ShowInCatalog,
            sellerId = string.IsNullOrWhiteSpace(request.SellerId) ? null : request.SellerId.Trim(),
            variations
        };
    }

    /// <summary>
    /// Gravity Create always Select()s Variations — null throws. Always send ≥1 SKU;
    /// when the seller has no color/size matrix, emit a single default variation.
    /// </summary>
    private static object[] BuildVariationsWire(GravityProductCreateRequest request)
    {
        var source = request.Variations is { Count: > 0 }
            ? request.Variations
            : new[]
            {
                new GravityProductVariationInput(
                    Name: request.Name,
                    VariantName: "Default",
                    Sku: null,
                    BasePrice: 0,
                    Stock: 0,
                    ImageUrl: request.ImageUrl)
            };

        return source.Select(v =>
        {
            var name = string.IsNullOrWhiteSpace(v.Name) ? request.Name : v.Name.Trim();
            object? varImages = null;
            var imageLink = !string.IsNullOrWhiteSpace(v.ImageUrl) ? v.ImageUrl : request.ImageUrl;
            if (!string.IsNullOrWhiteSpace(imageLink))
            {
                varImages = new[]
                {
                    new { link = imageLink.Trim(), isPrincipal = true }
                };
            }

            object? options = null;
            if (v.Options is { Count: > 0 })
            {
                options = v.Options
                    .Where(o => !string.IsNullOrWhiteSpace(o.Key) && !string.IsNullOrWhiteSpace(o.Name))
                    .Select(o => new
                    {
                        key = o.Key.Trim(),
                        name = o.Name.Trim(),
                        value = string.IsNullOrWhiteSpace(o.Value) ? null : o.Value.Trim()
                    })
                    .ToArray();
            }

            return (object)new
            {
                name,
                variantName = string.IsNullOrWhiteSpace(v.VariantName) ? name : v.VariantName.Trim(),
                sku = string.IsNullOrWhiteSpace(v.Sku) ? null : v.Sku.Trim(),
                basePrice = v.BasePrice < 0 ? 0 : v.BasePrice,
                height = v.Height <= 0 ? 1m : v.Height,
                length = v.Length <= 0 ? 1m : v.Length,
                width = v.Width <= 0 ? 1m : v.Width,
                weight = v.Weight <= 0 ? 1m : v.Weight,
                isActive = true,
                inventories = new[]
                {
                    new { stock = v.Stock < 0 ? 0 : v.Stock, sellerLocationId = (string?)null }
                },
                images = varImages,
                options
            };
        }).ToArray();
    }

    private async Task<JsonDocument> SendAsync(
        GravityStoreCredentials credentials,
        HttpMethod method,
        string relativePath,
        object? body,
        CancellationToken cancellationToken)
    {
        var (payload, _) = await SendRawAsync(credentials, method, relativePath, body, cancellationToken);
        return JsonDocument.Parse(string.IsNullOrWhiteSpace(payload) ? "{}" : payload);
    }

    private async Task<(string Payload, string? Location)> SendRawAsync(
        GravityStoreCredentials credentials,
        HttpMethod method,
        string relativePath,
        object? body,
        CancellationToken cancellationToken)
    {
        var client = httpClientFactory.CreateClient("GravityStoreData");
        var baseUrl = credentials.BaseUrl.Trim().TrimEnd('/') + "/";
        var requestUri = new Uri(new Uri(baseUrl), relativePath);
        var useBearer = !string.IsNullOrWhiteSpace(credentials.AccessToken);

        // Serialize once so the log line is exactly what goes on the wire.
        var bodyJson = body is null ? null : JsonSerializer.Serialize(body, JsonOptions);
        logger.LogInformation(
            "Gravity store request. {Method} {Url} Auth={Auth} Credential={CredentialMask} OrgId={OrgId} Body={Body}",
            method,
            requestUri,
            useBearer ? "Bearer" : DataIngestCredentials.GravityApiKeyHeader,
            DataIngestCredentials.MaskApiKey(useBearer ? credentials.AccessToken : credentials.ApiKey),
            credentials.OrganizationId,
            bodyJson is null ? "(none)" : Truncate(bodyJson));

        const int maxAttempts = 3;
        for (var attempt = 1; attempt <= maxAttempts; attempt++)
        {
            using var request = new HttpRequestMessage(method, requestUri);
            if (useBearer)
            {
                request.Headers.Authorization =
                    new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", credentials.AccessToken);
            }
            else
            {
                request.Headers.TryAddWithoutValidation(DataIngestCredentials.GravityApiKeyHeader, credentials.ApiKey);
            }

            request.Headers.TryAddWithoutValidation(
                DataIngestCredentials.GravityOrgIdHeader,
                credentials.OrganizationId);
            request.Headers.TryAddWithoutValidation(
                DataIngestCredentials.GravityCheckoutProviderTenantIdHeader,
                DataIngestCredentials.GravityCheckoutProviderTenantId);
            if (bodyJson is not null)
            {
                request.Content = new StringContent(bodyJson, System.Text.Encoding.UTF8, "application/json");
            }

            try
            {
                using var response = await client.SendAsync(request, cancellationToken);
                var payload = await response.Content.ReadAsStringAsync(cancellationToken);
                var location = response.Headers.Location?.ToString()
                    ?? (response.Headers.TryGetValues("Location", out var values)
                        ? values.FirstOrDefault()
                        : null);

                logger.LogInformation(
                    "Gravity store response. {Method} {Url} Status={Status} BodyLength={BodyLength} Preview={Preview}",
                    method,
                    requestUri,
                    (int)response.StatusCode,
                    payload.Length,
                    Truncate(payload));

                if (!response.IsSuccessStatusCode)
                {
                    logger.LogWarning(
                        "Gravity store {Method} {Path} failed ({Status}): {Body}",
                        method,
                        relativePath,
                        (int)response.StatusCode,
                        Truncate(payload));

                    var detail = TryReadProblemDetail(payload);
                    var message = string.IsNullOrWhiteSpace(detail)
                        ? $"Gravity store call failed ({(int)response.StatusCode}) for {relativePath}."
                        : $"Gravity store call failed ({(int)response.StatusCode}): {detail}";
                    throw new InvalidOperationException(message);
                }

                return (payload, location);
            }
            catch (HttpRequestException ex) when (attempt < maxAttempts && IsTransientTransport(ex))
            {
                logger.LogWarning(
                    ex,
                    "Gravity store transport reset on attempt {Attempt}/{MaxAttempts}. Retrying {Method} {Url}",
                    attempt,
                    maxAttempts,
                    method,
                    requestUri);
                await Task.Delay(TimeSpan.FromMilliseconds(200 * attempt), cancellationToken);
            }
            catch (HttpRequestException ex)
            {
                logger.LogError(
                    ex,
                    "Gravity store unreachable after {Attempts} attempt(s). {Method} {Url}",
                    attempt,
                    method,
                    requestUri);
                throw new InvalidOperationException(
                    $"Gravity store is unreachable ({requestUri.Host}). Connection was reset. Retry in a moment.",
                    ex);
            }
            catch (TaskCanceledException ex) when (!cancellationToken.IsCancellationRequested)
            {
                throw new InvalidOperationException(
                    $"Gravity store timed out for {relativePath}.",
                    ex);
            }
        }

        throw new InvalidOperationException($"Gravity store call failed for {relativePath}.");
    }

    private static GravityCartResult MapCartResult(string payload, string? location, string? fallbackCartId)
    {
        var trimmed = payload?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(trimmed) || trimmed == "{}")
        {
            var idFromLocation = TryReadCartIdFromLocation(location) ?? fallbackCartId;
            if (string.IsNullOrWhiteSpace(idFromLocation))
                throw new InvalidOperationException("Gravity create cart returned an empty body without a cart id.");
            return new GravityCartResult(idFromLocation, "{}", 0);
        }

        if (trimmed.Length >= 2 && trimmed[0] == '"' && trimmed[^1] == '"')
        {
            var id = JsonSerializer.Deserialize<string>(trimmed);
            if (!string.IsNullOrWhiteSpace(id))
                return new GravityCartResult(id, trimmed, 0);
        }

        using var doc = JsonDocument.Parse(trimmed);
        var root = doc.RootElement;
        if (root.ValueKind == JsonValueKind.String)
        {
            var id = root.GetString();
            if (!string.IsNullOrWhiteSpace(id))
                return new GravityCartResult(id, trimmed, 0);
        }

        var cartId = ReadString(root, "id")
            ?? ReadString(root, "uniqueId")
            ?? TryReadCartIdFromLocation(location)
            ?? fallbackCartId
            ?? string.Empty;

        var itemCount = 0;
        if (root.TryGetProperty("items", out var items) && items.ValueKind == JsonValueKind.Array)
            itemCount = items.GetArrayLength();

        return new GravityCartResult(
            cartId,
            trimmed,
            itemCount,
            ReadDecimal(root, "subTotal"),
            ReadDecimal(root, "shipping"),
            ReadDecimal(root, "tax"),
            ReadDecimal(root, "discount"),
            ReadDecimal(root, "total"),
            ReadString(root, "couponCode"),
            root.TryGetProperty("availableToBuy", out var atb) && atb.ValueKind is JsonValueKind.True or JsonValueKind.False
                ? atb.GetBoolean()
                : null);
    }

    private static string? TryReadCartIdFromLocation(string? location)
    {
        if (string.IsNullOrWhiteSpace(location))
            return null;
        try
        {
            var path = location.Contains("://", StringComparison.Ordinal)
                ? new Uri(location).AbsolutePath
                : location;
            var parts = path.Trim('/').Split('/', StringSplitOptions.RemoveEmptyEntries);
            var cartsIdx = Array.FindIndex(parts, p => p.Equals("Carts", StringComparison.OrdinalIgnoreCase));
            if (cartsIdx >= 0 && cartsIdx + 1 < parts.Length)
                return Uri.UnescapeDataString(parts[cartsIdx + 1]);
            return parts.Length > 0 ? Uri.UnescapeDataString(parts[^1]) : null;
        }
        catch (UriFormatException)
        {
            return null;
        }
    }

    private static bool IsTransientTransport(HttpRequestException ex)
    {
        if (ex.InnerException is IOException)
            return true;
        if (ex.InnerException is System.Net.Sockets.SocketException)
            return true;
        var text = ex.Message + (ex.InnerException?.Message ?? string.Empty);
        return text.Contains("Connection reset", StringComparison.OrdinalIgnoreCase)
               || text.Contains("forcibly closed", StringComparison.OrdinalIgnoreCase)
               || text.Contains("transport connection", StringComparison.OrdinalIgnoreCase);
    }

    private static string? TryReadProblemDetail(string payload)
    {
        if (string.IsNullOrWhiteSpace(payload))
            return null;
        try
        {
            using var doc = JsonDocument.Parse(payload);
            if (doc.RootElement.TryGetProperty("Detail", out var detail)
                && detail.ValueKind == JsonValueKind.String)
                return detail.GetString();
            if (doc.RootElement.TryGetProperty("detail", out detail)
                && detail.ValueKind == JsonValueKind.String)
                return detail.GetString();
            if (doc.RootElement.TryGetProperty("Title", out var title)
                && title.ValueKind == JsonValueKind.String)
                return title.GetString();
            if (doc.RootElement.TryGetProperty("title", out title)
                && title.ValueKind == JsonValueKind.String)
                return title.GetString();
        }
        catch (JsonException)
        {
            // ignore malformed problem payloads
        }

        return null;
    }

    private static IReadOnlyList<GravityBrandItem> MapBrandList(JsonElement root)
    {
        var results = new List<GravityBrandItem>();
        if (root.TryGetProperty("results", out var arr) && arr.ValueKind == JsonValueKind.Array)
        {
            foreach (var item in arr.EnumerateArray())
                results.Add(MapBrand(item));
        }
        else if (root.ValueKind == JsonValueKind.Array)
        {
            foreach (var item in root.EnumerateArray())
                results.Add(MapBrand(item));
        }

        return results;
    }

    private static GravityBrandItem MapBrand(JsonElement item)
    {
        var id = ReadString(item, "brandId") ?? ReadString(item, "id") ?? string.Empty;
        var isActive = true;
        if (item.TryGetProperty("isActive", out var active) && active.ValueKind is JsonValueKind.True or JsonValueKind.False)
            isActive = active.GetBoolean();
        return new GravityBrandItem(id, ReadString(item, "name"), isActive);
    }

    private static GravityBrandItem MapBrandWrite(string payload, string? location, string fallbackName)
    {
        if (!string.IsNullOrWhiteSpace(payload) && payload.TrimStart().StartsWith('{'))
        {
            using var doc = JsonDocument.Parse(payload);
            var mapped = MapBrand(doc.RootElement);
            if (!string.IsNullOrWhiteSpace(mapped.BrandId))
                return mapped with { Name = mapped.Name ?? fallbackName };
        }

        var idFromLocation = TryReadEntityIdFromLocation(location, "Brands");
        return new GravityBrandItem(idFromLocation ?? string.Empty, fallbackName);
    }

    private static GravityCategoryItem MapCategoryWrite(string payload, string? location, string fallbackName)
    {
        if (!string.IsNullOrWhiteSpace(payload) && payload.TrimStart().StartsWith('{'))
        {
            using var doc = JsonDocument.Parse(payload);
            var mapped = MapCategory(doc.RootElement);
            if (!string.IsNullOrWhiteSpace(mapped.CategoryId))
                return mapped with { Name = mapped.Name ?? fallbackName };
        }

        var idFromLocation = TryReadEntityIdFromLocation(location, "Categories");
        return new GravityCategoryItem(idFromLocation ?? string.Empty, fallbackName);
    }

    private static string? TryReadEntityIdFromLocation(string? location, string resourceSegment)
    {
        if (string.IsNullOrWhiteSpace(location))
            return null;
        try
        {
            var path = location.Contains("://", StringComparison.Ordinal)
                ? new Uri(location).AbsolutePath
                : location;
            var parts = path.Trim('/').Split('/', StringSplitOptions.RemoveEmptyEntries);
            var idx = Array.FindIndex(
                parts,
                p => p.Equals(resourceSegment, StringComparison.OrdinalIgnoreCase));
            if (idx >= 0 && idx + 1 < parts.Length)
                return Uri.UnescapeDataString(parts[idx + 1]);
            return parts.Length > 0 ? Uri.UnescapeDataString(parts[^1]) : null;
        }
        catch (UriFormatException)
        {
            return null;
        }
    }

    private static object BuildEntityAttributeBody(GravityEntityAttributeWriteRequest request) => new
    {
        entityName = request.EntityName,
        name = request.Name,
        description = request.Description,
        label = request.Label,
        values = request.Values,
        isMultiOption = request.IsMultiOption,
        specificationType = request.SpecificationType,
        order = request.Order,
        required = request.Required,
        isPublic = request.IsPublic,
        sectionGroup = request.SectionGroup
    };

    private static IReadOnlyList<GravityEntityAttributeItem> MapEntityAttributeList(JsonElement root)
    {
        var results = new List<GravityEntityAttributeItem>();
        if (root.TryGetProperty("results", out var arr) && arr.ValueKind == JsonValueKind.Array)
        {
            foreach (var item in arr.EnumerateArray())
                results.Add(MapEntityAttribute(item));
        }
        else if (root.ValueKind == JsonValueKind.Array)
        {
            foreach (var item in root.EnumerateArray())
                results.Add(MapEntityAttribute(item));
        }

        return results;
    }

    private static GravityEntityAttributeItem MapEntityAttribute(JsonElement item)
    {
        var id = ReadString(item, "entityAttributeId") ?? ReadString(item, "id") ?? string.Empty;
        return new GravityEntityAttributeItem(
            id,
            ReadString(item, "entityName"),
            ReadString(item, "key"),
            ReadString(item, "name"),
            ReadString(item, "description"),
            ReadString(item, "label"),
            ReadString(item, "values"),
            ReadBool(item, "isMultiOption") ?? false,
            ReadInt(item, "specificationType") ?? 0,
            ReadString(item, "specificationTypeName"),
            ReadInt(item, "order") ?? 0,
            ReadBool(item, "required") ?? false,
            ReadBool(item, "isPublic") ?? false,
            ReadString(item, "sectionGroup"));
    }

    private static IReadOnlyList<GravityCategoryItem> MapCategoryList(JsonElement root)
    {
        var results = new List<GravityCategoryItem>();
        if (root.ValueKind == JsonValueKind.Array)
        {
            foreach (var item in root.EnumerateArray())
                results.Add(MapCategory(item));
        }
        else if (root.TryGetProperty("results", out var arr) && arr.ValueKind == JsonValueKind.Array)
        {
            foreach (var item in arr.EnumerateArray())
                results.Add(MapCategory(item));
        }

        return results;
    }

    /// <summary>
    /// Flattens GET Categories/all (ProductCategoryModel tree with nested children).
    /// </summary>
    private static IReadOnlyList<GravityCategoryItem> FlattenCategoryTree(JsonElement root)
    {
        var results = new List<GravityCategoryItem>();
        IEnumerable<JsonElement> tops;
        if (root.ValueKind == JsonValueKind.Array)
            tops = root.EnumerateArray();
        else if (root.TryGetProperty("results", out var arr) && arr.ValueKind == JsonValueKind.Array)
            tops = arr.EnumerateArray();
        else
            return results;

        void Walk(JsonElement node, string? parentId)
        {
            var id = ReadString(node, "categoryId") ?? ReadString(node, "id");
            if (string.IsNullOrWhiteSpace(id))
                return;
            var name = ReadString(node, "name");
            var explicitParent = ReadString(node, "parentId") ?? ReadString(node, "parentCategoryId");
            results.Add(new GravityCategoryItem(
                id.Trim(),
                name,
                string.IsNullOrWhiteSpace(explicitParent) ? parentId : explicitParent.Trim(),
                ReadString(node, "url")));

            if (!node.TryGetProperty("children", out var kids) || kids.ValueKind != JsonValueKind.Array)
                return;
            foreach (var child in kids.EnumerateArray())
                Walk(child, id.Trim());
        }

        foreach (var top in tops)
            Walk(top, parentId: null);

        return results;
    }

    private static GravityCategoryItem MapCategory(JsonElement item)
    {
        var id = ReadString(item, "categoryId") ?? ReadString(item, "id") ?? string.Empty;
        return new GravityCategoryItem(
            id,
            ReadString(item, "name"),
            ReadString(item, "parentId") ?? ReadString(item, "parentCategoryId"),
            ReadString(item, "url"));
    }

    private static GravityProductWriteResult MapProductWrite(JsonElement item)
    {
        var id = ReadString(item, "productId") ?? ReadString(item, "id") ?? string.Empty;
        return new GravityProductWriteResult(id, ReadString(item, "name"), item.GetRawText());
    }

    private static string Slugify(string value)
    {
        var trimmed = value.Trim().ToLowerInvariant();
        var chars = trimmed.Select(c => char.IsLetterOrDigit(c) ? c : '-').ToArray();
        var slug = new string(chars);
        while (slug.Contains("--", StringComparison.Ordinal))
            slug = slug.Replace("--", "-", StringComparison.Ordinal);
        return slug.Trim('-');
    }

    private static GravityProductListItem MapProductListItem(JsonElement item)
    {
        var id = ReadString(item, "productId") ?? string.Empty;
        var (basePrice, specialPrice) = ReadPrices(item);
        return new GravityProductListItem(
            id,
            ReadString(item, "name"),
            ReadString(item, "productStatusName"),
            ReadDate(item, "updatedOn"),
            item.GetRawText(),
            ReadFirstImageUrl(item),
            ReadString(item, "sellerName") ?? ReadString(item, "principalSellerName"),
            ReadString(item, "brandName"),
            ReadString(item, "categoryPath"),
            ReadString(item, "productExternalId"),
            ReadDecimal(item, "totalStock"),
            basePrice,
            specialPrice,
            ReadString(item, "currencySymbol") ?? ReadString(item, "currency"));
    }

    private static GravityProductDetail MapProductDetail(JsonElement item)
    {
        var id = ReadString(item, "productId") ?? string.Empty;
        return new GravityProductDetail(
            id,
            ReadString(item, "name"),
            ReadString(item, "productStatusName"),
            ReadDate(item, "updatedOn"),
            item.GetRawText());
    }

    private static (decimal? BasePrice, decimal? SpecialPrice) ReadPrices(JsonElement item)
    {
        decimal? basePrice = null;
        decimal? specialPrice = null;

        if (item.TryGetProperty("prices", out var prices) && prices.ValueKind == JsonValueKind.Object)
        {
            basePrice = ReadDecimal(prices, "maxBasePrice") ?? ReadDecimal(prices, "minBasePrice");
            specialPrice = ReadDecimal(prices, "minSpecialPrice") ?? ReadDecimal(prices, "maxSpecialPrice");
        }

        basePrice ??= ReadDecimal(item, "totalPrice");
        var totalSpecial = ReadDecimal(item, "totalSpecialPrice");
        if (totalSpecial is > 0)
            specialPrice ??= totalSpecial;

        if (item.TryGetProperty("skus", out var skus) && skus.ValueKind == JsonValueKind.Array)
        {
            foreach (var sku in skus.EnumerateArray())
            {
                basePrice ??= ReadDecimal(sku, "basePrice");
                var skuSpecial = ReadDecimal(sku, "specialPrice");
                if (skuSpecial is > 0)
                    specialPrice ??= skuSpecial;
                if (basePrice is not null && specialPrice is not null)
                    break;
            }
        }

        if (specialPrice is 0)
            specialPrice = null;

        return (basePrice, specialPrice);
    }

    private static string? ReadFirstImageUrl(JsonElement item)
    {
        if (!item.TryGetProperty("skus", out var skus) || skus.ValueKind != JsonValueKind.Array)
            return null;

        foreach (var sku in skus.EnumerateArray())
        {
            if (!sku.TryGetProperty("imageList", out var images) || images.ValueKind != JsonValueKind.Array)
                continue;
            foreach (var image in images.EnumerateArray())
            {
                var link = ReadString(image, "link");
                if (!string.IsNullOrWhiteSpace(link))
                    return link;
            }
        }

        return null;
    }

    private static GravityOrderItem MapOrder(JsonElement item)
    {
        var id = ReadString(item, "saleOrderId") ?? string.Empty;
        var orderNumber = ReadString(item, "orderReferenceNumber")
            ?? ReadString(item, "saleOrderGroup")
            ?? id;
        var status = ReadString(item, "saleOrderCustomStatusName")
            ?? ReadString(item, "saleOrderStatusName")
            ?? ReadString(item, "status")
            ?? ReadString(item, "orderType")
            ?? ReadString(item, "shippingOrderType");

        string? clientName = null;
        string? clientSecondary = null;
        if (item.TryGetProperty("client", out var client) && client.ValueKind == JsonValueKind.Object)
        {
            clientName = ReadString(client, "fullName")
                ?? JoinNames(ReadString(client, "firstName"), ReadString(client, "lastName"))
                ?? ReadString(client, "entityName");
            clientSecondary = ReadString(client, "phone")
                ?? ReadString(client, "identificationNumber")
                ?? ReadString(client, "entityIdentificationNumber");
        }

        string? sellerName = null;
        if (item.TryGetProperty("seller", out var seller) && seller.ValueKind == JsonValueKind.Object)
            sellerName = ReadString(seller, "name");

        var providers = new List<string>();
        if (item.TryGetProperty("saleOrderProviderTenants", out var tenants) && tenants.ValueKind == JsonValueKind.Array)
        {
            foreach (var tenant in tenants.EnumerateArray())
            {
                var providerName = ReadString(tenant, "providerName");
                if (!string.IsNullOrWhiteSpace(providerName) && !providers.Contains(providerName, StringComparer.OrdinalIgnoreCase))
                    providers.Add(providerName);
            }
        }

        return new GravityOrderItem(
            id,
            orderNumber,
            status,
            ReadDate(item, "updatedOn") ?? ReadDate(item, "orderDate"),
            item.GetRawText(),
            orderNumber,
            ReadDate(item, "orderDate"),
            ReadDate(item, "shippingDate")
                ?? ReadDate(item, "shippingOrderDate")
                ?? ReadDate(item, "completedOrderDate"),
            clientName,
            clientSecondary,
            sellerName,
            ReadOrderItemCount(item),
            ReadDecimal(item, "total"),
            ReadString(item, "currencySymbol") ?? ReadString(item, "currencyIsoCode"),
            providers.Count > 0 ? providers : null);
    }

    private static int? ReadOrderItemCount(JsonElement item)
    {
        if (!item.TryGetProperty("saleOrderItems", out var items) || items.ValueKind != JsonValueKind.Array)
            return null;

        decimal totalQty = 0;
        var hasQty = false;
        var count = 0;
        foreach (var line in items.EnumerateArray())
        {
            count++;
            var qty = ReadDecimal(line, "qty");
            if (qty is null) continue;
            hasQty = true;
            totalQty += qty.Value;
        }

        if (count == 0) return 0;
        if (hasQty) return (int)Math.Round(totalQty, MidpointRounding.AwayFromZero);
        return count;
    }

    private static string? JoinNames(string? first, string? last)
    {
        var value = $"{first} {last}".Trim();
        return string.IsNullOrWhiteSpace(value) ? null : value;
    }

    private static string? ReadString(JsonElement el, string name)
        => el.TryGetProperty(name, out var p) && p.ValueKind == JsonValueKind.String ? p.GetString() : null;

    private static bool? ReadBool(JsonElement el, string name)
    {
        if (!el.TryGetProperty(name, out var p))
            return null;
        if (p.ValueKind is JsonValueKind.True or JsonValueKind.False)
            return p.GetBoolean();
        return null;
    }

    private static int? ReadInt(JsonElement el, string name)
    {
        if (!el.TryGetProperty(name, out var p))
            return null;
        if (p.ValueKind == JsonValueKind.Number && p.TryGetInt32(out var i))
            return i;
        if (p.ValueKind == JsonValueKind.String && int.TryParse(p.GetString(), out var parsed))
            return parsed;
        return null;
    }

    private static decimal? ReadDecimal(JsonElement el, string name)
    {
        if (!el.TryGetProperty(name, out var p))
            return null;
        if (p.ValueKind == JsonValueKind.Number && p.TryGetDecimal(out var d))
            return d;
        if (p.ValueKind == JsonValueKind.String && decimal.TryParse(p.GetString(), out var parsed))
            return parsed;
        return null;
    }

    private static DateTimeOffset? ReadDate(JsonElement el, string name)
    {
        if (!el.TryGetProperty(name, out var p))
            return null;
        if (p.ValueKind == JsonValueKind.String && DateTimeOffset.TryParse(p.GetString(), out var dto))
            return dto;
        return null;
    }

    private static string Truncate(string value)
        => value.Length <= 500 ? value : value[..500] + "…";
}
