using Microsoft.Extensions.AI;

namespace Commerce.Infrastructure.Agents.Definitions.DiscoveryAndCart.Tools;

internal static class ApplyCouponTool
{
    public const string Name = "apply_coupon";

    public static AITool Create(IServiceProvider services, CartSessionContext? session)
        => AIFunctionFactory.Create(
            async (string coupon_code, string? cart_id = null, CancellationToken cancellationToken = default) =>
            {
                if (string.IsNullOrWhiteSpace(coupon_code))
                    throw new ArgumentException("coupon_code is required.", nameof(coupon_code));

                var id = await CartToolSupport.RequireCartIdAsync(services, session, cart_id, cancellationToken)
                    .ConfigureAwait(false);
                var (client, credentials) = await CartToolSupport.ResolveGravityAsync(services, cancellationToken)
                    .ConfigureAwait(false);
                var cart = await client.ApplyCartCouponAsync(
                        credentials,
                        id,
                        coupon_code.Trim(),
                        cancellationToken)
                    .ConfigureAwait(false);
                return await CartToolSupport.RememberAndFormatAsync(services, session, cart, cancellationToken)
                    .ConfigureAwait(false);
            },
            name: Name,
            description: AgentTools.Description(services, Name));
}
