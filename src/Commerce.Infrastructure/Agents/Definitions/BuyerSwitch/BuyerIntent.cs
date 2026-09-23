using System.Text.Json.Serialization;
using Commerce.Application.Abstracts;

namespace Commerce.Infrastructure.Agents.Definitions.BuyerSwitch;

internal enum BuyerIntentKind
{
    Search,
    AddCart,
    UpdateCart,
    RemoveItem,
    Checkout,
    PostSales,
    Faq
}

/// <summary>
/// Structured output of the buyer intent classifier (YAML schema) plus turn routing facts.
/// Cart/user text are stamped outside the switch predicates (never a Build-time profile closure).
/// </summary>
internal sealed class BuyerIntent
{
    [JsonPropertyName("intent")]
    public BuyerIntentKind Intent { get; set; } = BuyerIntentKind.Search;

    [JsonPropertyName("product_name")]
    public string? ProductName { get; set; }

    [JsonPropertyName("confidence")]
    public double Confidence { get; set; }

    [JsonPropertyName("reason")]
    public string? Reason { get; set; }

    /// <summary>Fresh cart size at classify time (not LLM output).</summary>
    [JsonIgnore]
    public int CartItemCount { get; set; }

    [JsonIgnore]
    public string? CartId { get; set; }

    /// <summary>User utterance for specialists (seeded into WorkflowContext by the route entry).</summary>
    [JsonIgnore]
    public string UserText { get; set; } = string.Empty;
}
