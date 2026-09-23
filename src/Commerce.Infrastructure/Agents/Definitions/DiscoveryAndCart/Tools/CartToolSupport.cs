using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Commerce.Application.Abstracts;
using Commerce.Infrastructure.Agents.GravityCatalog;
using Commerce.Infrastructure.Agents.Profile;

namespace Commerce.Infrastructure.Agents.Definitions.DiscoveryAndCart.Tools;

/// <summary>
/// Turn identity captured when tools are built (CreateSpecialist).
/// MAF invokes tools without flowing AsyncLocal — do not rely on ChatTurnScope inside Invoke.
/// </summary>
internal sealed record CartSessionContext(string TenantId, string ThreadId, string Audience);

internal static class CartToolSupport
{
    public static CartSessionContext? TryCaptureSession()
    {
        var turn = ChatTurnScope.Current ?? ToolCallTurn.ResolveTurn();
        if (turn is null
            || string.IsNullOrWhiteSpace(turn.TenantId)
            || string.IsNullOrWhiteSpace(turn.ThreadId))
        {
            return null;
        }

        return new CartSessionContext(turn.TenantId, turn.ThreadId, turn.Audience);
    }

    public static CartSessionContext CaptureSessionOrThrow()
        => TryCaptureSession()
           ?? throw new InvalidOperationException(
               "Cart tools require an active chat turn (ChatTurnScope) when the specialist is created.");

    public static string RequireSkuId(string? skuId)
        => RequireId(skuId, "sku_id");

    public static string RequireId(string? value, string field)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException($"{field} is required (use search hit {field}).", field);
        var trimmed = value.Trim();
        if (trimmed.Contains('<', StringComparison.Ordinal)
            || trimmed.Contains('>', StringComparison.Ordinal)
            || string.Equals(trimmed, "sku_id", StringComparison.OrdinalIgnoreCase)
            || string.Equals(trimmed, "seller_id", StringComparison.OrdinalIgnoreCase)
            || string.Equals(trimmed, "product_id", StringComparison.OrdinalIgnoreCase))
        {
            throw new CartException(
                "invalid_id",
                $"{field} looks like a placeholder ({trimmed}). Use the real value from search_products.");
        }

        return trimmed;
    }

    /// <summary>
    /// Gravity often returns 200 + empty items when sellerId/skuId are wrong — surface that as an error.
    /// </summary>
    public static void EnsureItemsPresent(GravityCartResult cart, int expectedMinItems, string toolName)
    {
        if (cart.ItemCount >= expectedMinItems)
            return;

        var preview = string.IsNullOrWhiteSpace(cart.PayloadJson)
            ? "{}"
            : cart.PayloadJson.Length > 240 ? cart.PayloadJson[..240] : cart.PayloadJson;
        throw new CartException(
            CartException.EmptyItems,
            $"{toolName}: Gravity returned cartId={cart.CartId} with itemCount={cart.ItemCount} " +
            $"(expected >= {expectedMinItems}). Check sellerId/skuId. PayloadPreview={preview}");
    }

    public static async Task<(IGravityStoreDataClient Client, GravityStoreCredentials Credentials)> ResolveGravityAsync(
        IServiceProvider services,
        CancellationToken cancellationToken)
        => await GravityAgentContext.ResolveAsync(services, cancellationToken).ConfigureAwait(false);

    public static async Task<string?> TryGetCartIdAsync(
        IServiceProvider services,
        CartSessionContext? session,
        string? cartId,
        CancellationToken cancellationToken)
    {
        if (!string.IsNullOrWhiteSpace(cartId))
            return cartId.Trim();

        if (session is null
            || string.IsNullOrWhiteSpace(session.TenantId)
            || string.IsNullOrWhiteSpace(session.ThreadId))
        {
            return null;
        }

        var store = services.GetRequiredService<ISessionBuyerProfileStore>();
        var profile = await store.GetAsync(session.TenantId, session.ThreadId, cancellationToken)
            .ConfigureAwait(false);
        return string.IsNullOrWhiteSpace(profile.CartId) ? null : profile.CartId;
    }

    public static async Task<string> RequireCartIdAsync(
        IServiceProvider services,
        CartSessionContext? session,
        string? cartId,
        CancellationToken cancellationToken)
    {
        var id = await TryGetCartIdAsync(services, session, cartId, cancellationToken).ConfigureAwait(false);
        if (!string.IsNullOrWhiteSpace(id))
            return id;

        if (session is null)
        {
            throw new CartException(
                CartException.MissingSession,
                "No chat session for cart. Pass cart_id or run inside a buyer chat turn.");
        }

        throw new CartException(
            CartException.MissingCart,
            "No cart in session. Call add_item or create_cart first (they create and store cart_id).");
    }

    public static async Task<string> RememberAndFormatAsync(
        IServiceProvider services,
        CartSessionContext? session,
        GravityCartResult cart,
        CancellationToken cancellationToken)
    {
        var logger = services.GetRequiredService<ILoggerFactory>().CreateLogger("CartToolSupport");
        var json = Format(cart);

        if (string.IsNullOrWhiteSpace(cart.CartId))
        {
            logger.LogWarning(
                "Remember cart skipped: empty CartId from Gravity. JsonPreview={Preview}",
                json.Length > 160 ? json[..160] : json);
            return json;
        }

        if (session is null
            || !string.Equals(session.Audience, "buyer", StringComparison.OrdinalIgnoreCase))
        {
            logger.LogWarning(
                "Remember cart skipped: no CartSessionContext (AsyncLocal lost). cartId={CartId} — capture session at tool Create.",
                cart.CartId);
            return json;
        }

        try
        {
            var store = services.GetRequiredService<ISessionBuyerProfileStore>();
            var current = await store.GetAsync(session.TenantId, session.ThreadId, cancellationToken)
                .ConfigureAwait(false);
            var next = current with
            {
                TenantId = session.TenantId,
                ThreadId = session.ThreadId,
                CartId = cart.CartId,
                CartItemCount = Math.Max(0, cart.ItemCount),
                UpdatedAt = DateTimeOffset.UtcNow
            };
            await store.SaveAsync(next, cancellationToken).ConfigureAwait(false);
            logger.LogInformation(
                "Buyer cart session saved. ThreadId={ThreadId} CartId={CartId} Items={Items}",
                session.ThreadId,
                next.CartId,
                next.CartItemCount);
        }
        catch (Exception ex)
        {
            logger.LogError(
                ex,
                "Buyer cart session save failed. ThreadId={ThreadId} CartId={CartId}",
                session.ThreadId,
                cart.CartId);
            throw;
        }

        return json;
    }

    public static string FormatEmptyCart()
        => Format(new GravityCartResult(CartId: string.Empty, PayloadJson: "{}", ItemCount: 0));

    public static string FormatError(CartException ex)
        => GravityAgentContext.Json(new { error = ex.Code, message = ex.Message, cart = (object?)null });

    public static string Format(GravityCartResult cart)
    {
        var items = new List<object>();
        var paymentMethods = new List<object>();
        var shippingAvailables = new List<object>();
        string? currency = null;
        string? currencySymbol = null;
        string? country = null;

        try
        {
            using var doc = JsonDocument.Parse(
                string.IsNullOrWhiteSpace(cart.PayloadJson) ? "{}" : cart.PayloadJson);
            var root = doc.RootElement;
            currency = ReadString(root, "currencyIsoCode");
            currencySymbol = ReadString(root, "currencySymbol");
            country = ReadString(root, "countryIsoCode");

            if (root.TryGetProperty("items", out var arr) && arr.ValueKind == JsonValueKind.Array)
            {
                foreach (var item in arr.EnumerateArray())
                    items.Add(MapCartLine(item));
            }

            if (root.TryGetProperty("paymentAvailables", out var payments)
                && payments.ValueKind == JsonValueKind.Array)
            {
                foreach (var group in payments.EnumerateArray())
                {
                    if (!group.TryGetProperty("paymentMethods", out var methods)
                        || methods.ValueKind != JsonValueKind.Array
                        || methods.GetArrayLength() == 0)
                    {
                        continue;
                    }

                    var groupId = ReadString(group, "id");
                    var groupName = ReadString(group, "name");
                    var groupDescription = ReadString(group, "description");
                    foreach (var method in methods.EnumerateArray())
                    {
                        if (method.ValueKind != JsonValueKind.Object)
                            continue;
                        var methodId = ReadString(method, "id");
                        var methodName = ReadString(method, "name") ?? ReadString(method, "description");
                        if (string.IsNullOrWhiteSpace(methodId) && string.IsNullOrWhiteSpace(methodName))
                            continue;

                        paymentMethods.Add(new
                        {
                            group_id = groupId,
                            group_name = groupName,
                            group_description = groupDescription,
                            payment_method_id = methodId,
                            name = methodName,
                            description = ReadString(method, "description"),
                            code = ReadString(method, "code"),
                            require_checkout_session = ReadBool(method, "requireCheckOutSession")
                        });
                    }
                }
            }

            if (root.TryGetProperty("shippingAvailables", out var shipping)
                && shipping.ValueKind == JsonValueKind.Array)
            {
                foreach (var entry in shipping.EnumerateArray())
                {
                    if (entry.ValueKind != JsonValueKind.Object)
                        continue;
                    var sellerId = ReadString(entry, "sellerId");
                    var sellerName = ReadString(entry, "sellerName");
                    var options = new List<object>();
                    if (entry.TryGetProperty("options", out var opts) && opts.ValueKind == JsonValueKind.Array)
                    {
                        foreach (var opt in opts.EnumerateArray())
                        {
                            if (opt.ValueKind != JsonValueKind.Object)
                                continue;
                            options.Add(new
                            {
                                id = ReadString(opt, "id") ?? ReadString(opt, "optionId"),
                                name = ReadString(opt, "name") ?? ReadString(opt, "description"),
                                cost = ReadDecimal(opt, "cost") ?? ReadDecimal(opt, "price") ?? ReadDecimal(opt, "shippingCost")
                            });
                        }
                    }

                    shippingAvailables.Add(new
                    {
                        seller_id = sellerId,
                        seller_name = sellerName,
                        options_count = options.Count,
                        options,
                        has_shipping = entry.TryGetProperty("shipping", out var ship)
                            && ship.ValueKind is not JsonValueKind.Null and not JsonValueKind.Undefined
                    });
                }
            }
        }
        catch (JsonException)
        {
            // Payload is already counted in ItemCount; keep empty collections for the model view.
        }

        return GravityAgentContext.Json(new
        {
            cart_id = string.IsNullOrWhiteSpace(cart.CartId) ? null : cart.CartId,
            item_count = cart.ItemCount,
            currency,
            currency_symbol = currencySymbol,
            country,
            sub_total = cart.SubTotal,
            shipping = cart.Shipping,
            tax = cart.Tax,
            discount = cart.Discount,
            total = cart.Total,
            coupon_code = cart.CouponCode,
            available_to_buy = cart.AvailableToBuy,
            items,
            payment_methods = paymentMethods,
            payment_methods_count = paymentMethods.Count,
            shipping_availables = shippingAvailables,
            shipping_availables_count = shippingAvailables.Count
        });
    }

    private static object MapCartLine(JsonElement item)
    {
        string? skuId = null;
        string? skuCode = null;
        string? imageUrl = null;
        decimal? unitPrice = null;
        string? variantName = null;
        if (item.TryGetProperty("variants", out var variants)
            && variants.ValueKind == JsonValueKind.Array
            && variants.GetArrayLength() > 0)
        {
            var first = variants[0];
            skuId = ReadString(first, "skuId");
            skuCode = ReadString(first, "sku");
            imageUrl = ReadString(first, "skuImage");
            variantName = ReadString(first, "name");
            unitPrice = ReadDecimal(first, "finalPrice")
                ?? ReadDecimal(first, "specialPrice")
                ?? ReadDecimal(first, "basePrice");
        }

        return new
        {
            item_id = ReadString(item, "itemId") ?? ReadString(item, "id"),
            name = ReadString(item, "name") ?? variantName,
            quantity = ReadInt(item, "quantity"),
            seller_id = ReadString(item, "sellerId"),
            seller_name = ReadString(item, "sellerName"),
            sku_id = skuId,
            sku = skuCode,
            image_url = imageUrl,
            unit_price = unitPrice,
            sub_total = ReadDecimal(item, "subTotal"),
            discount = ReadDecimal(item, "discount"),
            total = ReadDecimal(item, "total")
        };
    }

    private static string? ReadString(JsonElement el, string name)
    {
        if (!el.TryGetProperty(name, out var p))
            return null;
        return p.ValueKind switch
        {
            JsonValueKind.String => p.GetString(),
            JsonValueKind.Number or JsonValueKind.True or JsonValueKind.False => p.ToString(),
            _ => null
        };
    }

    private static int? ReadInt(JsonElement el, string name)
    {
        if (!el.TryGetProperty(name, out var p))
            return null;
        if (p.ValueKind == JsonValueKind.Number && p.TryGetInt32(out var i))
            return i;
        if (p.ValueKind == JsonValueKind.String && int.TryParse(p.GetString(), out var parsed))
            return parsed;
        return null;
    }

    private static decimal? ReadDecimal(JsonElement el, string name)
    {
        if (!el.TryGetProperty(name, out var p))
            return null;
        if (p.ValueKind == JsonValueKind.Number && p.TryGetDecimal(out var d))
            return d;
        if (p.ValueKind == JsonValueKind.String && decimal.TryParse(p.GetString(), out var parsed))
            return parsed;
        return null;
    }

    private static bool? ReadBool(JsonElement el, string name)
    {
        if (!el.TryGetProperty(name, out var p))
            return null;
        return p.ValueKind switch
        {
            JsonValueKind.True => true,
            JsonValueKind.False => false,
            _ => null
        };
    }

    public static async Task<GravityCartResult> RefreshCartIfNeededAsync(
        IGravityStoreDataClient client,
        GravityStoreCredentials credentials,
        GravityCartResult cart,
        ILogger logger,
        string toolName,
        CancellationToken cancellationToken)
    {
        if (cart.ItemCount > 0 || string.IsNullOrWhiteSpace(cart.CartId))
            return cart;

        try
        {
            return await client.GetCartAsync(credentials, cart.CartId, cancellationToken)
                .ConfigureAwait(false);
        }
        catch (InvalidOperationException ex)
        {
            logger.LogWarning(
                ex,
                "{Tool}: GetCart after create failed; keeping create payload. cartId={CartId}",
                toolName,
                cart.CartId);
            return cart;
        }
    }
}
