using Microsoft.Extensions.AI;
using Microsoft.Extensions.DependencyInjection;
using Commerce.Application.Abstracts;

namespace Commerce.Infrastructure.Agents.Definitions.DiscoveryAndCart.Tools;

internal static class CompareProductsTool
{
    public const string Name = "compare_products";

    public static AITool Create(IServiceProvider services, string tenantId)
    {
        var index = services.GetRequiredService<ICatalogBrainIndex>();
        var catalog = services.GetRequiredService<ICatalogDataService>();
        return AIFunctionFactory.Create(
            (string[]? product_names = null, string[]? product_ids = null, CancellationToken cancellationToken = default) =>
                DiscoveryCatalogSearch.CompareAsync(index, catalog, tenantId, product_names, product_ids, cancellationToken),
            name: Name,
            description: AgentTools.Description(services, Name));
    }
}
