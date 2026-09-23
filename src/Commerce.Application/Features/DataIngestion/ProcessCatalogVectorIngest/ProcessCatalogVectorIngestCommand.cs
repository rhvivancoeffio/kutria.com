using Commerce.Application.Common.Abstracts;

namespace Commerce.Application.Features.DataIngestion.ProcessCatalogVectorIngest;

public sealed record ProcessCatalogVectorIngestCommand(
    Guid IntegrationId,
    string ProductId,
    string ExpectedContentHash,
    long DequeueCount = 1) : ICommand<ProcessCatalogVectorIngestResult>;
