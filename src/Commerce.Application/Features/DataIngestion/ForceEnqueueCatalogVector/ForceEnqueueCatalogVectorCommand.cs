using Commerce.Application.Common.Abstracts;

namespace Commerce.Application.Features.DataIngestion.ForceEnqueueCatalogVector;

/// <summary>
/// Re-enqueue an existing Table catalog row onto <c>catalog-vector-ingest</c>
/// (text upsert + image queue) without re-fetching from Gravity.
/// </summary>
public sealed record ForceEnqueueCatalogVectorCommand(Guid IntegrationId, string ProductId)
    : ICommand<ForceEnqueueCatalogVectorResult>;
