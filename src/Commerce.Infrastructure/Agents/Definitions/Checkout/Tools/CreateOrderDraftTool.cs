using Microsoft.Extensions.AI;
using Microsoft.Extensions.DependencyInjection;
using Commerce.Application.Abstracts;
using Commerce.Infrastructure.Agents.Definitions.DiscoveryAndCart.Tools;

namespace Commerce.Infrastructure.Agents.Definitions.Checkout.Tools;

internal static class CreateOrderDraftTool
{
    public const string Name = "create_order_draft";

    public static AITool Create(IServiceProvider services, CartSessionContext? session)
        => AIFunctionFactory.Create(
            async (string? cart_id = null, CancellationToken cancellationToken = default) =>
            {
                var id = await CartToolSupport.RequireCartIdAsync(services, session, cart_id, cancellationToken)
                    .ConfigureAwait(false);
                var (client, credentials) = await CartToolSupport.ResolveGravityAsync(services, cancellationToken)
                    .ConfigureAwait(false);
                var cart = await client.CheckoutCartAsync(credentials, id, cancellationToken)
                    .ConfigureAwait(false);

                var updater = services.GetRequiredService<ISessionBuyerProfileUpdater>();
                await updater.SetCheckoutFlagsAsync(
                        hasPendingOrder: true,
                        draftOrderId: cart.CartId,
                        cancellationToken: cancellationToken)
                    .ConfigureAwait(false);

                return await CartToolSupport.RememberAndFormatAsync(services, session, cart, cancellationToken)
                    .ConfigureAwait(false);
            },
            name: Name,
            description: AgentTools.Description(services, Name));
}
