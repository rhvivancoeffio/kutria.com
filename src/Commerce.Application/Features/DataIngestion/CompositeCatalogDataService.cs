using Commerce.Application.Abstracts;

namespace Commerce.Application.Features.DataIngestion;

public sealed class CompositeCatalogDataService(IIngestedCatalogMemory ingested) : ICatalogDataService
{
    public Task<IReadOnlyList<CatalogSkuSnapshot>> GetBySkusAsync(
        string tenantId,
        IReadOnlyList<string> skus,
        CancellationToken cancellationToken = default)
        => Task.FromResult(ingested.GetBySkus(tenantId, skus));
}
