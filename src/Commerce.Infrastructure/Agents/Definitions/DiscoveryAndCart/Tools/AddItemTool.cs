using Microsoft.Extensions.AI;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Commerce.Application.Abstracts;

namespace Commerce.Infrastructure.Agents.Definitions.DiscoveryAndCart.Tools;

internal static class AddItemTool
{
    public const string Name = "add_item";

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
                if (quantity <= 0)
                    throw new ArgumentException("quantity must be > 0.", nameof(quantity));

                var sku = CartToolSupport.RequireSkuId(sku_id);
                var seller = CartToolSupport.RequireId(seller_id, "seller_id");
                var logger = services.GetRequiredService<ILoggerFactory>().CreateLogger("AddItemTool");
                var (client, credentials) = await CartToolSupport.ResolveGravityAsync(services, cancellationToken)
                    .ConfigureAwait(false);
                var item = new GravityCartItemInput(sku, quantity, seller);
                var resolvedCartId = await CartToolSupport.TryGetCartIdAsync(services, session, cart_id, cancellationToken)
                    .ConfigureAwait(false);

                GravityCartResult cart;
                if (string.IsNullOrWhiteSpace(resolvedCartId))
                {
                    logger.LogInformation(
                        "add_item: no session cart — CreateCartAsync sku={Sku} qty={Qty} seller={Seller} session={Session}",
                        sku,
                        quantity,
                        seller,
                        session is null ? "(none)" : $"{session.TenantId}/{session.ThreadId}");
                    cart = await client.CreateCartAsync(
                            credentials,
                            new GravityCartCreateRequest([item], PostalCode: postal_code),
                            cancellationToken)
                        .ConfigureAwait(false);
                    cart = await CartToolSupport.RefreshCartIfNeededAsync(
                            client, credentials, cart, logger, Name, cancellationToken)
                        .ConfigureAwait(false);
                }
                else
                {
                    logger.LogInformation(
                        "add_item: AddCartItemsAsync cartId={CartId} sku={Sku} qty={Qty} seller={Seller}",
                        resolvedCartId,
                        sku,
                        quantity,
                        seller);
                    cart = await client.AddCartItemsAsync(
                            credentials,
                            resolvedCartId,
                            [item],
                            postal_code,
                            cancellationToken)
                        .ConfigureAwait(false);
                }

                CartToolSupport.EnsureItemsPresent(cart, expectedMinItems: 1, Name);
                return await CartToolSupport.RememberAndFormatAsync(services, session, cart, cancellationToken)
                    .ConfigureAwait(false);
            },
            name: Name,
            description: AgentTools.Description(services, Name));
}
