using Microsoft.Extensions.AI;

namespace Commerce.Infrastructure.Agents.Definitions.DiscoveryAndCart.Tools;

internal static class RemoveCouponTool
{
    public const string Name = "remove_coupon";

    public static AITool Create(IServiceProvider services, CartSessionContext? session)
        => AIFunctionFactory.Create(
            async (string? cart_id = null, CancellationToken cancellationToken = default) =>
            {
                var id = await CartToolSupport.RequireCartIdAsync(services, session, cart_id, cancellationToken)
                    .ConfigureAwait(false);
                var (client, credentials) = await CartToolSupport.ResolveGravityAsync(services, cancellationToken)
                    .ConfigureAwait(false);
                var cart = await client.RemoveCartCouponAsync(credentials, id, cancellationToken)
                    .ConfigureAwait(false);
                return await CartToolSupport.RememberAndFormatAsync(services, session, cart, cancellationToken)
                    .ConfigureAwait(false);
            },
            name: Name,
            description: AgentTools.Description(services, Name));
}
