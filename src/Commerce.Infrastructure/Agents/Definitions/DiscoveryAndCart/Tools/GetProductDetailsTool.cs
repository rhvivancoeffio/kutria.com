using System.Text.Json;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.DependencyInjection;
using Commerce.Application.Abstracts;
using Commerce.Infrastructure.Agents.Definitions.DiscoveryAndCart;

namespace Commerce.Infrastructure.Agents.Definitions.DiscoveryAndCart.Tools;

internal static class GetProductDetailsTool
{
    public const string Name = "get_product_details";

    public static AITool Create(IServiceProvider services, string tenantId)
    {
        var catalog = services.GetRequiredService<ICatalogDataService>();
        var updater = services.GetRequiredService<ISessionBuyerProfileUpdater>();
        return AIFunctionFactory.Create(
            async (string product_id, CancellationToken cancellationToken) =>
            {
                var json = await CatalogSnapshotLookup.GetAsync(catalog, tenantId, product_id, cancellationToken)
                    .ConfigureAwait(false);
                await TryRememberProductAsync(updater, json, cancellationToken).ConfigureAwait(false);
                return json;
            },
            name: Name,
            description: AgentTools.Description(services, Name));
    }

    private static async Task TryRememberProductAsync(
        ISessionBuyerProfileUpdater updater,
        string json,
        CancellationToken cancellationToken)
    {
        try
        {
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;
            if (!root.TryGetProperty("found", out var found) || found.ValueKind != JsonValueKind.True)
                return;

            var id = root.TryGetProperty("product_id", out var pid) ? pid.GetString() : null;
            var name = root.TryGetProperty("name", out var n) ? n.GetString() : id;
            if (string.IsNullOrWhiteSpace(id))
                return;

            await updater.SetLastViewedProductAsync(id, name, cancellationToken).ConfigureAwait(false);
        }
        catch (JsonException)
        {
            // best-effort
        }
    }
}
