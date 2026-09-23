using System.Collections.Concurrent;
using Commerce.Application.Abstracts;

namespace Commerce.Application.Features.DataIngestion;

public sealed class IngestedCatalogMemory : IIngestedCatalogMemory
{
    private readonly ConcurrentDictionary<string, ConcurrentDictionary<string, CatalogSkuSnapshot>> _tenants =
        new(StringComparer.OrdinalIgnoreCase);

    public void UpsertSkus(string tenantId, IReadOnlyList<CatalogSkuSnapshot> snapshots)
    {
        if (string.IsNullOrWhiteSpace(tenantId) || snapshots.Count == 0)
            return;

        var map = _tenants.GetOrAdd(tenantId, _ => new ConcurrentDictionary<string, CatalogSkuSnapshot>(StringComparer.OrdinalIgnoreCase));
        foreach (var snapshot in snapshots)
        {
            if (!string.IsNullOrWhiteSpace(snapshot.Sku))
                map[snapshot.Sku] = snapshot;
        }
    }

    public IReadOnlyList<CatalogSkuSnapshot> GetBySkus(string tenantId, IReadOnlyList<string> skus)
    {
        if (string.IsNullOrWhiteSpace(tenantId) || !_tenants.TryGetValue(tenantId, out var map))
            return [];

        return skus
            .Where(sku => !string.IsNullOrWhiteSpace(sku) && map.ContainsKey(sku))
            .Select(sku => map[sku])
            .ToList();
    }
}
