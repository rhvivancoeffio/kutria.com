using Commerce.Application.Common.Abstracts;

namespace Commerce.Application.Features.DataIngestion.ProcessCatalogImageVector;

public sealed record ProcessCatalogImageVectorCommand(
    Guid IntegrationId,
    string ProductId,
    string ImageUrl,
    IReadOnlyList<string> Skus,
    string ExpectedContentHash,
    long DequeueCount = 1) : ICommand<ProcessCatalogImageVectorResult>;
