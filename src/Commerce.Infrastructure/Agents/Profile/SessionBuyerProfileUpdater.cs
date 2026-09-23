using Commerce.Application.Abstracts;
using Commerce.Infrastructure.Agents;
using Microsoft.Extensions.Logging;

namespace Commerce.Infrastructure.Agents.Profile;

public sealed class SessionBuyerProfileUpdater(
    ISessionBuyerProfileStore store,
    ILogger<SessionBuyerProfileUpdater> logger) : ISessionBuyerProfileUpdater
{
    public Task SetCartAsync(
        string? cartId,
        int itemCount,
        CancellationToken cancellationToken = default)
        => PatchAsync(
            current => current with
            {
                CartId = cartId,
                CartItemCount = Math.Max(0, itemCount)
            },
            cancellationToken);

    public Task SetLastViewedProductAsync(
        string? productId,
        string? productName,
        CancellationToken cancellationToken = default)
        => PatchAsync(
            current => current with
            {
                LastViewedProductId = productId,
                LastViewedProductName = productName
            },
            cancellationToken);

    public Task SetCheckoutFlagsAsync(
        bool hasPendingOrder,
        string? draftOrderId = null,
        bool? hasShippingAddress = null,
        CancellationToken cancellationToken = default)
        => PatchAsync(
            current => current with
            {
                HasPendingOrder = hasPendingOrder,
                DraftOrderId = draftOrderId ?? current.DraftOrderId,
                HasShippingAddress = hasShippingAddress ?? current.HasShippingAddress
            },
            cancellationToken);

    private async Task PatchAsync(
        Func<SessionBuyerProfile, SessionBuyerProfile> mutate,
        CancellationToken cancellationToken)
    {
        var turn = ToolCallTurn.ResolveTurn();
        if (turn is null
            || !string.Equals(turn.Audience, "buyer", StringComparison.OrdinalIgnoreCase))
        {
            logger.LogWarning(
                "Buyer profile patch skipped: no turn scope (ChatTurnScope/ToolCallTurn). Audience={Audience}",
                turn?.Audience ?? "(null)");
            return;
        }

        try
        {
            var current = await store.GetAsync(turn.TenantId, turn.ThreadId, cancellationToken)
                .ConfigureAwait(false);
            var next = mutate(current) with
            {
                TenantId = turn.TenantId,
                ThreadId = turn.ThreadId,
                UpdatedAt = DateTimeOffset.UtcNow
            };
            await store.SaveAsync(next, cancellationToken).ConfigureAwait(false);
            logger.LogInformation(
                "Buyer profile saved. ThreadId={ThreadId} CartId={CartId} CartItems={CartItems}",
                turn.ThreadId,
                next.CartId ?? "(none)",
                next.CartItemCount);
        }
        catch (Exception ex)
        {
            logger.LogWarning(
                ex,
                "Buyer profile patch failed for thread {ThreadId}",
                turn.ThreadId);
        }
    }
}
