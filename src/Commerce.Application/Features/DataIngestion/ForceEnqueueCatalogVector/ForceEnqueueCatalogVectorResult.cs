namespace Commerce.Application.Features.DataIngestion.ForceEnqueueCatalogVector;

public sealed record ForceEnqueueCatalogVectorResult(
    Guid IntegrationId,
    string ProductId,
    string ContentHash);
