using Commerce.Application.Abstracts;

namespace Commerce.Application.Abstracts;

public interface IIngestedCatalogMemory
{
    void UpsertSkus(string tenantId, IReadOnlyList<CatalogSkuSnapshot> snapshots);

    IReadOnlyList<CatalogSkuSnapshot> GetBySkus(string tenantId, IReadOnlyList<string> skus);
}
