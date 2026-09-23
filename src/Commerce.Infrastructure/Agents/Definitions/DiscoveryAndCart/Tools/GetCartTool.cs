using Microsoft.Extensions.AI;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Commerce.Infrastructure.Agents.Definitions.DiscoveryAndCart.Tools;

internal static class GetCartTool
{
    public const string Name = "get_cart";

    public static AITool Create(IServiceProvider services, CartSessionContext? session)
        => AIFunctionFactory.Create(
            async (string? cart_id = null, CancellationToken cancellationToken = default) =>
            {
                var logger = services.GetRequiredService<ILoggerFactory>().CreateLogger("GetCartTool");
                var id = await CartToolSupport.TryGetCartIdAsync(services, session, cart_id, cancellationToken)
                    .ConfigureAwait(false);

                if (string.IsNullOrWhiteSpace(id))
                {
                    logger.LogInformation(
                        "get_cart: no cart in session; returning empty. session={Session}",
                        session is null ? "(none)" : $"{session.TenantId}/{session.ThreadId}");
                    return CartToolSupport.FormatEmptyCart();
                }

                logger.LogInformation("get_cart: cartId={CartId}", id);
                var (client, credentials) = await CartToolSupport.ResolveGravityAsync(services, cancellationToken)
                    .ConfigureAwait(false);
                var cart = await client.GetCartAsync(credentials, id, cancellationToken)
                    .ConfigureAwait(false);
                return await CartToolSupport.RememberAndFormatAsync(services, session, cart, cancellationToken)
                    .ConfigureAwait(false);
            },
            name: Name,
            description: AgentTools.Description(services, Name));
}
