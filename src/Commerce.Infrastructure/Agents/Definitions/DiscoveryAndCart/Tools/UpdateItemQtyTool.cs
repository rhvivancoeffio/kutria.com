using Microsoft.Extensions.AI;
using Commerce.Application.Abstracts;

namespace Commerce.Infrastructure.Agents.Definitions.DiscoveryAndCart.Tools;

internal static class UpdateItemQtyTool
{
    public const string Name = "update_item_qty";

    public static AITool Create(IServiceProvider services, CartSessionContext? session)
        => AIFunctionFactory.Create(
            async (
                string sku_id,
                int quantity,
                string seller_id,
                string? cart_id = null,
                string? postal_code = null,
                CancellationToken cancellationToken = default) =>
            {
                if (quantity < 0)
                    throw new ArgumentException("quantity must be >= 0.", nameof(quantity));

                var sku = CartToolSupport.RequireSkuId(sku_id);
                var seller = CartToolSupport.RequireId(seller_id, "seller_id");
                var id = await CartToolSupport.RequireCartIdAsync(services, session, cart_id, cancellationToken)
                    .ConfigureAwait(false);
                var (client, credentials) = await CartToolSupport.ResolveGravityAsync(services, cancellationToken)
                    .ConfigureAwait(false);
                var cart = await client.UpdateCartItemQuantitiesAsync(
                        credentials,
                        id,
                        [new GravityCartItemInput(sku, quantity, seller)],
                        postal_code,
                        cancellationToken)
                    .ConfigureAwait(false);
                return await CartToolSupport.RememberAndFormatAsync(services, session, cart, cancellationToken)
                    .ConfigureAwait(false);
            },
            name: Name,
            description: AgentTools.Description(services, Name));
}
