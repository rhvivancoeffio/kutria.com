using Commerce.Application.Abstracts;

namespace Commerce.Infrastructure.Agents.Tools;

public sealed class UnavailableCatalogDataService : ICatalogDataService
{
    public Task<IReadOnlyList<CatalogSkuSnapshot>> GetBySkusAsync(
        string tenantId,
        IReadOnlyList<string> skus,
        CancellationToken cancellationToken = default)
        => Task.FromResult<IReadOnlyList<CatalogSkuSnapshot>>([]);
}
