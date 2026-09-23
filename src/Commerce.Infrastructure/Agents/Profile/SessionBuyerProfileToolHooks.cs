using System.Text.Json;
using Commerce.Application.Abstracts;

namespace Commerce.Infrastructure.Agents.Profile;

/// <summary>
/// Best-effort profile patches when cart tools return success-shaped JSON (stubs stay no-ops).
/// </summary>
internal static class SessionBuyerProfileToolHooks
{
    public static async Task TryApplyCartPatchAsync(
        ISessionBuyerProfileUpdater updater,
        string result,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(result)
            || result.Contains("not implemented", StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        try
        {
            using var doc = JsonDocument.Parse(result);
            var root = doc.RootElement;
            string? cartId = null;
            var itemCount = 0;
            var hasCount = false;

            if (root.TryGetProperty("cart_id", out var cartIdEl)
                || root.TryGetProperty("cartId", out cartIdEl)
                || root.TryGetProperty("id", out cartIdEl))
            {
                cartId = cartIdEl.GetString();
            }

            if (root.TryGetProperty("item_count", out var countEl)
                || root.TryGetProperty("itemCount", out countEl)
                || root.TryGetProperty("cart_item_count", out countEl))
            {
                if (countEl.TryGetInt32(out var n))
                {
                    itemCount = n;
                    hasCount = true;
                }
            }
            else if (root.TryGetProperty("items", out var items) && items.ValueKind == JsonValueKind.Array)
            {
                itemCount = items.GetArrayLength();
                hasCount = true;
            }

            if (!hasCount && cartId is null)
                return;

            await updater.SetCartAsync(cartId, itemCount, cancellationToken).ConfigureAwait(false);
        }
        catch (JsonException)
        {
            // best-effort
        }
    }
}
