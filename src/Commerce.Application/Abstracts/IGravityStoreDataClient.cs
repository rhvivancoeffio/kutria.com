namespace Commerce.Application.Abstracts;

public sealed record GravityStoreCredentials(
    string BaseUrl,
    string ApiKey,
    string OrganizationId,
    string? DefaultSellerId = null,
    /// <summary>
    /// When set, store API calls use <c>Authorization: Bearer</c> (native Gravity).
    /// When null/empty, calls use <c>X-Gravity-API-Key</c> (GravityAPI).
    /// </summary>
    string? AccessToken = null);

public sealed record GravityProductPage(
    int CurrentPage,
    int PageCount,
    int PageSize,
    int RowCount,
    IReadOnlyList<GravityProductListItem> Results);

public sealed record GravityProductListItem(
    string ProductId,
    string? Name,
    string? ProductStatusName,
    DateTimeOffset? UpdatedOn,
    string PayloadJson,
    string? ImageUrl = null,
    string? SellerName = null,
    string? BrandName = null,
    string? CategoryPath = null,
    string? MarketplaceId = null,
    decimal? Stock = null,
    decimal? BasePrice = null,
    decimal? SpecialPrice = null,
    string? CurrencySymbol = null);

public sealed record GravityProductDetail(
    string ProductId,
    string? Name,
    string? ProductStatusName,
    DateTimeOffset? UpdatedOn,
    string PayloadJson);

public sealed record GravityOrderPage(
    int CurrentPage,
    int PageSize,
    bool HasMore,
    IReadOnlyList<GravityOrderItem> Results);

public sealed record GravityOrderItem(
    string SaleOrderId,
    string? Name,
    string? Status,
    DateTimeOffset? UpdatedOn,
    string PayloadJson,
    string? OrderNumber = null,
    DateTimeOffset? OrderDate = null,
    DateTimeOffset? DeliveryDate = null,
    string? ClientName = null,
    string? ClientSecondary = null,
    string? SellerName = null,
    int? ItemCount = null,
    decimal? Total = null,
    string? CurrencySymbol = null,
    IReadOnlyList<string>? ProviderNames = null);

public sealed record GravityBrandItem(string BrandId, string? Name, bool IsActive = true);

public sealed record GravityBrandCreateRequest(
    string Name,
    string? Description = null,
    bool IsActive = true,
    string? ImageUrl = null);

public sealed record GravityEntityAttributeItem(
    string EntityAttributeId,
    string? EntityName = null,
    string? Key = null,
    string? Name = null,
    string? Description = null,
    string? Label = null,
    string? Values = null,
    bool IsMultiOption = false,
    int SpecificationType = 0,
    string? SpecificationTypeName = null,
    int Order = 0,
    bool Required = false,
    bool IsPublic = false,
    string? SectionGroup = null);

public sealed record GravityEntityAttributeWriteRequest(
    string EntityName,
    string Name,
    bool IsMultiOption,
    int SpecificationType,
    bool Required,
    string? Description = null,
    string? Label = null,
    string? Values = null,
    int Order = 0,
    bool IsPublic = false,
    string? SectionGroup = null);

public sealed record GravityCategoryItem(
    string CategoryId,
    string? Name,
    string? ParentId = null,
    string? Url = null);

public sealed record GravityCategoryCreateRequest(
    string Name,
    string? Slug = null,
    string? Description = null,
    string? ParentCategoryId = null,
    bool IsActive = true);

public sealed record GravityProductCreateRequest(
    string Name,
    string Description,
    string? BrandId = null,
    string? BrandName = null,
    string? CategoryId = null,
    string? CategoryPath = null,
    string? ImageUrl = null,
    bool IsActive = true,
    bool ShowInCatalog = true,
    string? SellerId = null,
    IReadOnlyList<GravityProductVariationInput>? Variations = null);

/// <summary>SKU / variation line for Gravity ProductPostModel.Variations.</summary>
public sealed record GravityProductVariationInput(
    string Name,
    string? Sku = null,
    string? VariantName = null,
    decimal BasePrice = 0,
    int Stock = 0,
    string? ImageUrl = null,
    IReadOnlyList<GravityProductVariationOption>? Options = null,
    decimal Height = 1,
    decimal Length = 1,
    decimal Width = 1,
    decimal Weight = 1);

public sealed record GravityProductVariationOption(
    string Key,
    string Name,
    string? Value = null);

public sealed record GravityProductWriteResult(
    string ProductId,
    string? Name,
    string PayloadJson);

public sealed record GravityCartItemInput(
    string SkuId,
    int Quantity,
    string? SellerId = null);

public sealed record GravityCartCreateRequest(
    IReadOnlyList<GravityCartItemInput> Items,
    string? Email = null,
    string? Country = null,
    string? PostalCode = null,
    string? Source = null);

public sealed record GravityCartResult(
    string CartId,
    string PayloadJson,
    int ItemCount,
    decimal? SubTotal = null,
    decimal? Shipping = null,
    decimal? Tax = null,
    decimal? Discount = null,
    decimal? Total = null,
    string? CouponCode = null,
    bool? AvailableToBuy = null);

public interface IGravityStoreDataClient
{
    Task<GravityProductPage> ListProductsAsync(
        GravityStoreCredentials credentials,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default);

    Task<GravityProductDetail?> GetProductByIdAsync(
        GravityStoreCredentials credentials,
        string productId,
        CancellationToken cancellationToken = default);

    Task<GravityOrderPage> ListOrdersAsync(
        GravityStoreCredentials credentials,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default);

    Task<GravityOrderItem?> GetOrderByIdAsync(
        GravityStoreCredentials credentials,
        string orderId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<GravityBrandItem>> ListBrandsAsync(
        GravityStoreCredentials credentials,
        string? name = null,
        int page = 1,
        int pageSize = 20,
        CancellationToken cancellationToken = default);

    Task<GravityBrandItem?> GetBrandAsync(
        GravityStoreCredentials credentials,
        string brandId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<GravityBrandItem>> AutocompleteBrandsAsync(
        GravityStoreCredentials credentials,
        string name,
        CancellationToken cancellationToken = default);

    Task<GravityBrandItem> CreateBrandAsync(
        GravityStoreCredentials credentials,
        GravityBrandCreateRequest request,
        CancellationToken cancellationToken = default);

    Task<GravityBrandItem> UpdateBrandAsync(
        GravityStoreCredentials credentials,
        string brandId,
        GravityBrandCreateRequest request,
        CancellationToken cancellationToken = default);

    Task DeleteBrandAsync(
        GravityStoreCredentials credentials,
        string brandId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<GravityEntityAttributeItem>> ListEntityAttributesAsync(
        GravityStoreCredentials credentials,
        string? name = null,
        int page = 1,
        int pageSize = 20,
        CancellationToken cancellationToken = default);

    Task<GravityEntityAttributeItem?> GetEntityAttributeAsync(
        GravityStoreCredentials credentials,
        string entityAttributeId,
        CancellationToken cancellationToken = default);

    Task<GravityEntityAttributeItem> CreateEntityAttributeAsync(
        GravityStoreCredentials credentials,
        GravityEntityAttributeWriteRequest request,
        CancellationToken cancellationToken = default);

    Task<GravityEntityAttributeItem> UpdateEntityAttributeAsync(
        GravityStoreCredentials credentials,
        string entityAttributeId,
        GravityEntityAttributeWriteRequest request,
        CancellationToken cancellationToken = default);

    Task DeleteEntityAttributeAsync(
        GravityStoreCredentials credentials,
        string entityAttributeId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<GravityCategoryItem>> ListCategoriesAsync(
        GravityStoreCredentials credentials,
        string? name = null,
        CancellationToken cancellationToken = default);

    Task<GravityCategoryItem?> GetCategoryAsync(
        GravityStoreCredentials credentials,
        string categoryId,
        CancellationToken cancellationToken = default);

    Task<GravityCategoryItem> CreateCategoryAsync(
        GravityStoreCredentials credentials,
        GravityCategoryCreateRequest request,
        CancellationToken cancellationToken = default);

    Task<GravityCategoryItem> UpdateCategoryAsync(
        GravityStoreCredentials credentials,
        string categoryId,
        GravityCategoryCreateRequest request,
        CancellationToken cancellationToken = default);

    Task DeleteCategoryAsync(
        GravityStoreCredentials credentials,
        string categoryId,
        CancellationToken cancellationToken = default);

    Task<GravityProductWriteResult> CreateProductAsync(
        GravityStoreCredentials credentials,
        GravityProductCreateRequest request,
        CancellationToken cancellationToken = default);

    Task<GravityProductWriteResult> UpdateProductAsync(
        GravityStoreCredentials credentials,
        string productId,
        GravityProductCreateRequest request,
        CancellationToken cancellationToken = default);

    Task DeleteProductAsync(
        GravityStoreCredentials credentials,
        string productId,
        CancellationToken cancellationToken = default);

    Task PublishProductAsync(
        GravityStoreCredentials credentials,
        string productId,
        CancellationToken cancellationToken = default);

    Task<GravityCartResult> CreateCartAsync(
        GravityStoreCredentials credentials,
        GravityCartCreateRequest request,
        CancellationToken cancellationToken = default);

    Task<GravityCartResult> GetCartAsync(
        GravityStoreCredentials credentials,
        string cartId,
        CancellationToken cancellationToken = default);

    Task<GravityCartResult> CheckoutCartAsync(
        GravityStoreCredentials credentials,
        string cartId,
        CancellationToken cancellationToken = default);

    Task<GravityCartResult> AddCartItemsAsync(
        GravityStoreCredentials credentials,
        string cartId,
        IReadOnlyList<GravityCartItemInput> items,
        string? postalCode = null,
        CancellationToken cancellationToken = default);

    Task<GravityCartResult> UpdateCartItemQuantitiesAsync(
        GravityStoreCredentials credentials,
        string cartId,
        IReadOnlyList<GravityCartItemInput> items,
        string? postalCode = null,
        CancellationToken cancellationToken = default);

    Task<GravityCartResult> ApplyCartCouponAsync(
        GravityStoreCredentials credentials,
        string cartId,
        string couponCode,
        CancellationToken cancellationToken = default);

    Task<GravityCartResult> RemoveCartCouponAsync(
        GravityStoreCredentials credentials,
        string cartId,
        CancellationToken cancellationToken = default);

    Task<GravityCartResult> RemoveCartItemAsync(
        GravityStoreCredentials credentials,
        string cartId,
        string itemId,
        CancellationToken cancellationToken = default);

    /// <summary>GET /sellers/default — principal seller for cart line items.</summary>
    Task<GravityDefaultSeller> GetDefaultSellerAsync(
        GravityStoreCredentials credentials,
        CancellationToken cancellationToken = default);
}

public sealed record GravityDefaultSeller(string SellerId, string? Name);
