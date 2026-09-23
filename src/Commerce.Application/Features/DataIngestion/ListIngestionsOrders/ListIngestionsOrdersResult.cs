namespace Commerce.Application.Features.DataIngestion.ListIngestionsOrders;

public sealed record ListIngestionsOrderItem(
    string Id,
    string? Name,
    string? Status,
    DateTimeOffset? UpdatedOn,
    DateTimeOffset SyncedAt,
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

public sealed record ListIngestionsOrdersResult(
    IReadOnlyList<ListIngestionsOrderItem> Items,
    int PageSize,
    string? NextToken,
    bool HasMore);
