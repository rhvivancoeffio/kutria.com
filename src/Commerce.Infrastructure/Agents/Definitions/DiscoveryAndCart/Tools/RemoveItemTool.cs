using Microsoft.Extensions.AI;

namespace Commerce.Infrastructure.Agents.Definitions.DiscoveryAndCart.Tools;

internal static class RemoveItemTool
{
    public const string Name = "remove_item";

    public static AITool Create(IServiceProvider services, CartSessionContext? session)
        => AIFunctionFactory.Create(
            async (string item_id, string? cart_id = null, CancellationToken cancellationToken = default) =>
            {
                if (string.IsNullOrWhiteSpace(item_id))
                    throw new ArgumentException("item_id is required.", nameof(item_id));

                var id = await CartToolSupport.RequireCartIdAsync(services, session, cart_id, cancellationToken)
                    .ConfigureAwait(false);
                var (client, credentials) = await CartToolSupport.ResolveGravityAsync(services, cancellationToken)
                    .ConfigureAwait(false);
                var cart = await client.RemoveCartItemAsync(
                        credentials,
                        id,
                        item_id.Trim(),
                        cancellationToken)
                    .ConfigureAwait(false);
                return await CartToolSupport.RememberAndFormatAsync(services, session, cart, cancellationToken)
                    .ConfigureAwait(false);
            },
            name: Name,
            description: AgentTools.Description(services, Name));
}
