namespace Commerce.Application.Features.DataIngestion.ListIngestedCatalog;

public sealed record ListIngestedCatalogItem(
    string Id,
    string? Name,
    string? Status,
    DateTimeOffset? UpdatedOn,
    DateTimeOffset SyncedAt,
    string? ImageUrl = null,
    string? SellerName = null,
    string? BrandName = null,
    string? CategoryPath = null,
    string? MarketplaceId = null,
    decimal? Stock = null,
    decimal? BasePrice = null,
    decimal? SpecialPrice = null,
    string? CurrencySymbol = null);

public sealed record ListIngestedCatalogResult(
    IReadOnlyList<ListIngestedCatalogItem> Items,
    int Page,
    int PageSize,
    bool HasMore);
