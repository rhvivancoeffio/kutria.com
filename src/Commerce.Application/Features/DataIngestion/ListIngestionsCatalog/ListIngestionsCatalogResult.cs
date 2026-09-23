namespace Commerce.Application.Features.DataIngestion.ListIngestionsCatalog;

public sealed record ListIngestionsCatalogItem(
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

public sealed record ListIngestionsCatalogResult(
    IReadOnlyList<ListIngestionsCatalogItem> Items,
    int PageSize,
    string? NextToken,
    bool HasMore);
