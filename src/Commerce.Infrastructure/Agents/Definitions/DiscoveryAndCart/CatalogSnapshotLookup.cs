using System.Text.Json;
using Commerce.Application.Abstracts;

namespace Commerce.Infrastructure.Agents.Definitions.DiscoveryAndCart;

internal static class CatalogSnapshotLookup
{
    public static async Task<string> GetAsync(
        ICatalogDataService catalog,
        string tenantId,
        string sku,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(sku))
        {
            return JsonSerializer.Serialize(new { product_id = sku, found = false, error = "missing_product_id" });
        }

        var snapshots = await catalog.GetBySkusAsync(tenantId, [sku], cancellationToken);
        var match = snapshots.FirstOrDefault(item => string.Equals(item.Sku, sku, StringComparison.OrdinalIgnoreCase));
        if (match is null)
        {
            return JsonSerializer.Serialize(new { product_id = sku, found = false, error = "not_found" });
        }

        return JsonSerializer.Serialize(new
        {
            product_id = match.Sku,
            name = match.Sku,
            description = (string?)null,
            price = match.Price,
            currency = DiscoveryCatalogSearch.StoreCurrency,
            category = (string?)null,
            image_urls = Array.Empty<string>(),
            stock = match.Stock,
            status = match.Status,
            found = true,
            error = (string?)null
        });
    }
}
