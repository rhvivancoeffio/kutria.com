using Microsoft.Extensions.AI;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Commerce.Application.Abstracts;

namespace Commerce.Infrastructure.Agents.Definitions.DiscoveryAndCart.Tools;

internal static class SearchProductsTool
{
    public const string Name = "search_products";

    public static AITool Create(IServiceProvider services, string tenantId)
    {
        var index = services.GetRequiredService<ICatalogBrainIndex>();
        var catalog = services.GetRequiredService<ICatalogDataService>();
        var logger = services.GetRequiredService<ILoggerFactory>().CreateLogger("DiscoveryCatalogSearch");
        return AIFunctionFactory.Create(
            (
                string? query = null,
                string? category = null,
                decimal? price_max = null,
                decimal? budget = null,
                string? currency = null,
                string[]? items = null,
                string? occasion = null,
                string? reference_product_id = null,
                int? limit = null,
                string? gender = null,
                string? size = null,
                string? color = null,
                string[]? option_filters = null,
                CancellationToken cancellationToken = default) =>
                DiscoveryCatalogSearch.SearchAsync(
                    index,
                    catalog,
                    tenantId,
                    query,
                    category,
                    price_max,
                    budget,
                    currency,
                    items,
                    occasion,
                    reference_product_id,
                    limit,
                    cancellationToken,
                    logger,
                    CatalogOptionNormalizer.BuildFilters(gender, size, color, option_filters)),
            name: Name,
            description: AgentTools.Description(services, Name));
    }
}
