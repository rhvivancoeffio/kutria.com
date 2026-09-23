namespace Commerce.Application.Features.DataIngestion.ProcessCatalogVectorIngest;

public sealed record ProcessCatalogVectorIngestResult(
    Guid IntegrationId,
    string ProductId,
    string Outcome,
    int SkuCount);
