using System.Text.Json;
using Commerce.Application.Abstracts;
using Commerce.Infrastructure.Agents;
using Microsoft.Agents.AI;
using Microsoft.Extensions.Logging;

namespace Commerce.Infrastructure.Agents.Profile;

/// <summary>
/// Injects authoritative buyer session facts into MAF context (not routing if/else).
/// </summary>
public sealed class SessionBuyerProfileAIContextProvider(
    ISessionBuyerProfileStore store,
    ILogger<SessionBuyerProfileAIContextProvider> logger) : AIContextProvider
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web)
    {
        DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
    };

    protected override async ValueTask<AIContext> ProvideAIContextAsync(
        InvokingContext context,
        CancellationToken cancellationToken = default)
    {
        var turn = ToolCallTurn.ResolveTurn();
        if (turn is null
            || !string.Equals(turn.Audience, "buyer", StringComparison.OrdinalIgnoreCase))
        {
            return new AIContext();
        }

        try
        {
            var profile = await store.GetAsync(turn.TenantId, turn.ThreadId, cancellationToken)
                .ConfigureAwait(false);
            var payload = JsonSerializer.Serialize(
                new
                {
                    cart_id = profile.CartId,
                    cart_item_count = profile.CartItemCount,
                    last_viewed_product_id = profile.LastViewedProductId,
                    last_viewed_product_name = profile.LastViewedProductName,
                    has_pending_order = profile.HasPendingOrder,
                    draft_order_id = profile.DraftOrderId,
                    has_shipping_address = profile.HasShippingAddress
                },
                Json);

            logger.LogDebug(
                "Buyer session profile injected for thread {ThreadId} cartItems {CartItems}",
                turn.ThreadId,
                profile.CartItemCount);

            return new AIContext
            {
                Instructions =
                    $"Session state (authoritative): {payload}. " +
                    "Use this state when deciding tools; do not invent cart or product state."
            };
        }
        catch (Exception ex)
        {
            logger.LogDebug(ex, "Buyer session profile inject skipped for {ThreadId}", turn.ThreadId);
            return new AIContext();
        }
    }
}
