using Microsoft.Extensions.AI;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Commerce.Application.Abstracts;

namespace Commerce.Infrastructure.Agents.Definitions.DiscoveryAndCart.Tools;

internal static class CreateCartTool
{
    public const string Name = "create_cart";

    public static AITool Create(IServiceProvider services, CartSessionContext? session)
        => AIFunctionFactory.Create(
            async (
                string sku_id,
                int quantity,
                string seller_id,
                string? email = null,
                string? country = null,
                string? postal_code = null,
                string? source = null,
                CancellationToken cancellationToken = default) =>
            {
                if (quantity <= 0)
                    throw new ArgumentException("quantity must be > 0.", nameof(quantity));

                var sku = CartToolSupport.RequireSkuId(sku_id);
                var seller = CartToolSupport.RequireId(seller_id, "seller_id");
                var logger = services.GetRequiredService<ILoggerFactory>().CreateLogger("CreateCartTool");
                var (client, credentials) = await CartToolSupport.ResolveGravityAsync(services, cancellationToken)
                    .ConfigureAwait(false);
                logger.LogInformation(
                    "create_cart: sku={Sku} qty={Qty} seller={Seller} session={Session}",
                    sku,
                    quantity,
                    seller,
                    session is null ? "(none)" : $"{session.TenantId}/{session.ThreadId}");

                var cart = await client.CreateCartAsync(
                        credentials,
                        new GravityCartCreateRequest(
                            [new GravityCartItemInput(sku, quantity, seller)],
                            email,
                            country,
                            postal_code,
                            source),
                        cancellationToken)
                    .ConfigureAwait(false);

                cart = await CartToolSupport.RefreshCartIfNeededAsync(
                        client, credentials, cart, logger, Name, cancellationToken)
                    .ConfigureAwait(false);
                CartToolSupport.EnsureItemsPresent(cart, expectedMinItems: 1, Name);

                return await CartToolSupport.RememberAndFormatAsync(services, session, cart, cancellationToken)
                    .ConfigureAwait(false);
            },
            name: Name,
            description: AgentTools.Description(services, Name));
}
