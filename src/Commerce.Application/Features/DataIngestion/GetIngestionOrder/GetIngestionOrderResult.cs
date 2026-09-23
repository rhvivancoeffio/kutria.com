namespace Commerce.Application.Features.DataIngestion.GetIngestionOrder;

public sealed record GetIngestionOrderResult(
    string Id,
    string? Name,
    string? Status,
    DateTimeOffset? UpdatedOn,
    DateTimeOffset SyncedAt,
    string PayloadJson);
