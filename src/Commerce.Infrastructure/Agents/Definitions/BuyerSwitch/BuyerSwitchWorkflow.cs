using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Agents.AI;
using Microsoft.Extensions.Logging;
using Commerce.Application.Abstracts;

namespace Commerce.Infrastructure.Agents.Definitions.BuyerSwitch;

/// <summary>
/// Runtime B: YAML classifier + deterministic route (same rules as MAF AddSwitch).
/// Specialist LLM runs outside BindAsExecutor — MAF was canceling in-flight RunAsync.
/// </summary>
internal static class BuyerSwitchWorkflow
{
    public const string ClassifierId = "intent-classifier";
    private const double MinConfidence = 0.45;

    private static readonly JsonSerializerOptions IntentJson = new()
    {
        PropertyNameCaseInsensitive = true,
        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) }
    };

    public static async Task<BuyerIntent> ClassifyAsync(
        AIAgent classifierAgent,
        SessionBuyerProfile profile,
        string userText,
        ILogger logger,
        CancellationToken cancellationToken)
    {
        var classifyPrompt =
            $"""
            session_profile: cart_item_count={profile.CartItemCount}, cart_id={profile.CartId ?? "(none)"}, has_pending_order={profile.HasPendingOrder}, last_viewed_product={profile.LastViewedProductName ?? "(none)"}
            user_message: {userText}
            """;

        logger.LogInformation(
            "Buyer switch classifying cartItems={CartItems}",
            profile.CartItemCount);

        var response = await classifierAgent.RunAsync(classifyPrompt, cancellationToken: cancellationToken)
            .ConfigureAwait(false);
        var intent = ParseIntent(response.Text);
        if (intent.Confidence < MinConfidence)
            intent.Intent = BuyerIntentKind.Search;

        intent.CartItemCount = profile.CartItemCount;
        intent.CartId = profile.CartId;
        intent.UserText = userText;

        logger.LogInformation(
            "Buyer switch classified intent {Intent} confidence {Confidence} product {Product} cartItems {CartItems} reason {Reason}",
            intent.Intent,
            intent.Confidence,
            intent.ProductName ?? "(none)",
            intent.CartItemCount,
            intent.Reason ?? "(none)");

        return intent;
    }

    /// <summary>
    /// Same routing rules as the planned MAF AddSwitch (hard gates on CartItemCount).
    /// </summary>
    public static string ResolveSpecialistKey(BuyerIntent intent, IReadOnlyList<AgentDefinition> catalog)
    {
        var keys = catalog
            .Select(a => a.Key)
            .Where(k => !string.Equals(k, ClassifierId, StringComparison.OrdinalIgnoreCase))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        string Pick(string key, string fallback)
            => keys.Contains(key) ? key : fallback;

        var discovery = Require(catalog, "discovery-cart").Key;

        return intent.Intent switch
        {
            BuyerIntentKind.Search
                or BuyerIntentKind.AddCart
                or BuyerIntentKind.UpdateCart
                or BuyerIntentKind.RemoveItem
                => discovery,

            BuyerIntentKind.Checkout when intent.CartItemCount > 0
                => Pick("checkout", discovery),

            BuyerIntentKind.Checkout
                => discovery,

            BuyerIntentKind.PostSales
                => Pick("post-sales-support", discovery),

            BuyerIntentKind.Faq
                => Pick("corporate-faq", discovery),

            _ => discovery
        };
    }

    public static string BuildSpecialistPrompt(BuyerIntent intent)
    {
        var userText = intent.UserText ?? string.Empty;
        var hints = new List<string>();

        if (intent.Intent is BuyerIntentKind.Search)
        {
            hints.Add(
                "[router intent=search] Debes llamar search_products antes de responder. "
                + "No respondas solo con saludo o pidiendo más detalle: muestra productos del catálogo. "
                + "Si el pedido es abierto (qué tienes, qué ofreces, opciones, recomendaciones), usa query amplio (ej. \"productos\", categoría mencionada o el texto del usuario).");
        }

        if (!string.IsNullOrWhiteSpace(intent.ProductName)
            && (intent.Intent is BuyerIntentKind.Search or BuyerIntentKind.AddCart)
            && !userText.Contains(intent.ProductName, StringComparison.OrdinalIgnoreCase))
        {
            hints.Add($"[classifier product_name={intent.ProductName}]");
        }

        return hints.Count == 0
            ? userText
            : $"{userText}\n\n{string.Join("\n", hints)}";
    }

    private static AgentDefinition Require(IReadOnlyList<AgentDefinition> catalog, string key)
        => catalog.FirstOrDefault(a => string.Equals(a.Key, key, StringComparison.OrdinalIgnoreCase))
           ?? throw new InvalidOperationException($"Buyer switch requires agent '{key}' in the audience catalog.");

    private static BuyerIntent ParseIntent(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return new BuyerIntent { Intent = BuyerIntentKind.Search, Confidence = 0, Reason = "empty" };

        try
        {
            var parsed = JsonSerializer.Deserialize<BuyerIntent>(text, IntentJson);
            return parsed ?? new BuyerIntent { Intent = BuyerIntentKind.Search, Confidence = 0, Reason = "null_parse" };
        }
        catch (JsonException)
        {
            return new BuyerIntent { Intent = BuyerIntentKind.Search, Confidence = 0, Reason = "parse_failed" };
        }
    }
}
