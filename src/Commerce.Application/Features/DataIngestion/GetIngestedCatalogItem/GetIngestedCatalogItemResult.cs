namespace Commerce.Application.Features.DataIngestion.GetIngestedCatalogItem;

public sealed record GetIngestedCatalogItemResult(
    string Id,
    string? Name,
    string? Status,
    DateTimeOffset? UpdatedOn,
    DateTimeOffset SyncedAt,
    string PayloadJson);
