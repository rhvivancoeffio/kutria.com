namespace Commerce.Application.Features.DataIngestion.ProcessCatalogImageVector;

public sealed record ProcessCatalogImageVectorResult(
    Guid IntegrationId,
    string ProductId,
    string Outcome,
    int PatchedSkuCount);
