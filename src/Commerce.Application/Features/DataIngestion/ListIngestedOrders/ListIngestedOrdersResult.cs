namespace Commerce.Application.Features.DataIngestion.ListIngestedOrders;

public sealed record ListIngestedOrderItem(
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

public sealed record ListIngestedOrdersResult(
    IReadOnlyList<ListIngestedOrderItem> Items,
    int Page,
    int PageSize,
    bool HasMore);
