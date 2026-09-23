namespace Commerce.Application.Features.DataIngestion.GetIngestedOrder;

public sealed record GetIngestedOrderResult(
    string Id,
    string? Name,
    string? Status,
    DateTimeOffset? UpdatedOn,
    DateTimeOffset SyncedAt,
    string PayloadJson);
