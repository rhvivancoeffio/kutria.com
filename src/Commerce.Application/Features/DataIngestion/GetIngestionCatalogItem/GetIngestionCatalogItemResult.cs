namespace Commerce.Application.Features.DataIngestion.GetIngestionCatalogItem;

public sealed record GetIngestionCatalogItemResult(
    string Id,
    string? Name,
    string? Status,
    DateTimeOffset? UpdatedOn,
    DateTimeOffset SyncedAt,
    string PayloadJson);
