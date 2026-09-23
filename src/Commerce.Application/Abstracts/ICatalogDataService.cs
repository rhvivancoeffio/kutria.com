namespace Commerce.Application.Abstracts;

public interface ICatalogDataService
{
    Task<IReadOnlyList<CatalogSkuSnapshot>> GetBySkusAsync(
        string tenantId,
        IReadOnlyList<string> skus,
        CancellationToken cancellationToken = default);
}
