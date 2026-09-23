using System.Text;
using System.Text.Json;

namespace Commerce.Infrastructure.Agents;

internal static class AgentOutput
{
    public static string Canonical(JsonElement element)
        => JsonSerializer.Serialize(element);

    public static bool TryRead(string raw, out string json, out string? message, out string? error)
    {
        json = string.Empty;
        message = null;
        var text = Normalize(raw);
        if (string.IsNullOrWhiteSpace(text))
        {
            error = "Agent returned an empty response.";
            return false;
        }

        try
        {
            using var document = JsonDocument.Parse(text);
            if (document.RootElement.ValueKind != JsonValueKind.Object)
            {
                error = "Agent output must be a JSON object.";
                return false;
            }

            var root = Instance(document.RootElement);
            // Truncated model output often leaves ExtractLastObject with a nested product card.
            if (LooksLikeProductCard(root) && !LooksLikeAgentOutput(root))
            {
                text = WrapProducts([Canonical(root)]);
                using var wrapped = JsonDocument.Parse(text);
                root = wrapped.RootElement.Clone();
            }

            json = Canonical(root);
            if (root.TryGetProperty("message", out var messageElement)
                && messageElement.ValueKind == JsonValueKind.String)
            {
                message = messageElement.GetString();
            }
        }
        catch (JsonException)
        {
            error = $"Agent output is not valid JSON. {Preview(text)}";
            return false;
        }

        error = null;
        return true;
    }

    /// <summary>
    /// Models often emit products first and omit/truncate <c>message</c>. Fill a short default so the UI has text.
    /// </summary>
    public static string EnsureDisplayMessage(string json, out string? message)
    {
        message = null;
        try
        {
            using var document = JsonDocument.Parse(json);
            var root = document.RootElement;
            if (root.ValueKind != JsonValueKind.Object)
                return json;

            if (root.TryGetProperty("message", out var existing)
                && existing.ValueKind == JsonValueKind.String
                && !string.IsNullOrWhiteSpace(existing.GetString()))
            {
                message = existing.GetString();
                return json;
            }

            var productCount = 0;
            if (root.TryGetProperty("products", out var products)
                && products.ValueKind == JsonValueKind.Array)
            {
                productCount = products.GetArrayLength();
            }

            var hasCart = root.TryGetProperty("cart", out var cart)
                && cart.ValueKind == JsonValueKind.Object;

            message = productCount > 0
                ? $"Encontré {productCount} opciones."
                : hasCart
                    ? "Aquí está tu carrito."
                    : null;

            if (message is null)
                return json;

            using var stream = new MemoryStream();
            using (var writer = new Utf8JsonWriter(stream))
            {
                writer.WriteStartObject();
                var wroteMessage = false;
                foreach (var prop in root.EnumerateObject())
                {
                    if (prop.NameEquals("message"))
                    {
                        writer.WriteString("message", message);
                        wroteMessage = true;
                        continue;
                    }

                    prop.WriteTo(writer);
                }

                if (!wroteMessage)
                    writer.WriteString("message", message);
                writer.WriteEndObject();
            }

            return Encoding.UTF8.GetString(stream.ToArray());
        }
        catch (JsonException)
        {
            return json;
        }
    }

    public static string Placeholder(string agentName)
    {
        return JsonSerializer.Serialize(new Dictionary<string, string>
        {
            ["message"] = $"Placeholder reply from {agentName}. Azure OpenAI is not configured, so no model was called."
        });
    }

    private static JsonElement Instance(JsonElement root)
    {
        if (root.TryGetProperty("properties", out var properties)
            && properties.ValueKind == JsonValueKind.Object
            && properties.TryGetProperty("message", out _)
            && root.TryGetProperty("type", out var type)
            && type.ValueKind == JsonValueKind.String
            && string.Equals(type.GetString(), "object", StringComparison.OrdinalIgnoreCase))
        {
            return properties.Clone();
        }

        return root.Clone();
    }

    private static string Normalize(string raw)
    {
        var text = StripFence(raw);
        if (string.IsNullOrWhiteSpace(text))
            return text;

        // Prefer a complete agent envelope when present.
        if (TryParseObject(text, out var full) && LooksLikeAgentOutput(full))
            return text;

        var recovered = ExtractAgentOrProducts(text);
        if (recovered is not null)
            return recovered;

        // Last resort: previous behavior (may be a nested product card; TryRead wraps it).
        return ExtractLastObject(text) ?? text;
    }

    /// <summary>
    /// Prefer a full agent envelope. If the model truncated mid-products[], collect every complete
    /// product card and rebuild <c>{ type, message, products, cart }</c>.
    /// </summary>
    private static string? ExtractAgentOrProducts(string text)
    {
        string? bestAgent = null;
        var productCards = new List<string>();

        for (var i = 0; i < text.Length; i++)
        {
            if (text[i] != '{' || !TrySliceObject(text, i, out var end))
                continue;

            var slice = text[i..(end + 1)];
            i = end;
            if (!TryParseObject(slice, out var el))
                continue;

            if (LooksLikeAgentOutput(el))
                bestAgent = slice;
            else if (LooksLikeProductCard(el))
                productCards.Add(slice);
        }

        if (bestAgent is not null)
            return bestAgent;

        if (productCards.Count > 0)
            return WrapProducts(productCards);

        return null;
    }

    private static string WrapProducts(IReadOnlyList<string> productJsonObjects)
    {
        var products = new List<JsonElement>(productJsonObjects.Count);
        foreach (var card in productJsonObjects)
        {
            using var doc = JsonDocument.Parse(card);
            products.Add(doc.RootElement.Clone());
        }

        using var stream = new MemoryStream();
        using (var writer = new Utf8JsonWriter(stream))
        {
            writer.WriteStartObject();
            writer.WriteString("type", "products");
            writer.WriteString("message", $"Encontré {products.Count} opciones.");
            writer.WritePropertyName("products");
            writer.WriteStartArray();
            foreach (var product in products)
                product.WriteTo(writer);
            writer.WriteEndArray();
            writer.WriteNull("cart");
            writer.WriteEndObject();
        }

        return Encoding.UTF8.GetString(stream.ToArray());
    }

    private static bool LooksLikeAgentOutput(JsonElement root)
    {
        if (root.ValueKind != JsonValueKind.Object)
            return false;

        if (root.TryGetProperty("products", out var products) && products.ValueKind == JsonValueKind.Array)
            return true;

        if (root.TryGetProperty("cart", out var cart) && cart.ValueKind is JsonValueKind.Object or JsonValueKind.Null)
        {
            if (root.TryGetProperty("message", out _) || root.TryGetProperty("type", out _))
                return true;
        }

        if (root.TryGetProperty("type", out var type) && type.ValueKind == JsonValueKind.String)
        {
            var kind = type.GetString();
            if (string.Equals(kind, "products", StringComparison.OrdinalIgnoreCase)
                || string.Equals(kind, "cart", StringComparison.OrdinalIgnoreCase)
                || string.Equals(kind, "message", StringComparison.OrdinalIgnoreCase)
                || string.Equals(kind, "approval_needed", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        // Generative ficha payloads must not be rewritten as product cards.
        if (root.TryGetProperty("ficha", out var ficha) && ficha.ValueKind == JsonValueKind.Object)
            return true;

        if (root.TryGetProperty("images", out var images) && images.ValueKind == JsonValueKind.Array)
            return true;

        if (root.TryGetProperty("video", out var video) && video.ValueKind == JsonValueKind.Object)
            return true;

        if (LooksLikeGenerativeFicha(root))
            return true;

        return false;
    }

    /// <summary>
    /// Flat generative ficha (name + description + brandHint/categoryHint/seo/bullets) — not a catalog product card.
    /// </summary>
    private static bool LooksLikeGenerativeFicha(JsonElement root)
    {
        if (root.ValueKind != JsonValueKind.Object)
            return false;

        var hasName = root.TryGetProperty("name", out var name)
            && name.ValueKind == JsonValueKind.String
            && !string.IsNullOrWhiteSpace(name.GetString());
        var hasDescription = root.TryGetProperty("description", out var description)
            && description.ValueKind == JsonValueKind.String
            && !string.IsNullOrWhiteSpace(description.GetString());
        if (!hasName || !hasDescription)
            return false;

        if (root.TryGetProperty("brandHint", out _))
            return true;
        if (root.TryGetProperty("categoryHint", out _))
            return true;
        if (root.TryGetProperty("bullets", out var bullets) && bullets.ValueKind == JsonValueKind.Array)
            return true;
        if (root.TryGetProperty("seo", out var seo) && seo.ValueKind == JsonValueKind.Object)
            return true;

        return false;
    }

    private static bool LooksLikeProductCard(JsonElement root)
    {
        if (root.ValueKind != JsonValueKind.Object)
            return false;
        if (root.TryGetProperty("products", out _))
            return false;

        var hasSku = root.TryGetProperty("sku", out var sku)
            && sku.ValueKind == JsonValueKind.String
            && !string.IsNullOrWhiteSpace(sku.GetString());
        var hasName = root.TryGetProperty("name", out var name)
            && name.ValueKind == JsonValueKind.String
            && !string.IsNullOrWhiteSpace(name.GetString());
        return hasSku || hasName;
    }

    private static bool TryParseObject(string text, out JsonElement element)
    {
        element = default;
        try
        {
            using var document = JsonDocument.Parse(text);
            if (document.RootElement.ValueKind != JsonValueKind.Object)
                return false;
            element = document.RootElement.Clone();
            return true;
        }
        catch (JsonException)
        {
            return false;
        }
    }

    private static string Preview(string text)
    {
        var compact = text.Replace('\n', ' ').Replace('\r', ' ').Trim();
        return compact.Length <= 160 ? compact : compact[..160];
    }

    private static string? ExtractLastObject(string text)
    {
        string? last = null;
        for (var i = 0; i < text.Length; i++)
        {
            if (text[i] != '{' || !TrySliceObject(text, i, out var end))
                continue;

            last = text[i..(end + 1)];
            i = end;
        }

        return last;
    }

    private static bool TrySliceObject(string text, int start, out int end)
    {
        var depth = 0;
        var inString = false;
        var escaped = false;
        for (var i = start; i < text.Length; i++)
        {
            var ch = text[i];
            if (inString)
            {
                if (escaped)
                    escaped = false;
                else if (ch == '\\')
                    escaped = true;
                else if (ch == '"')
                    inString = false;
                continue;
            }

            if (ch == '"')
                inString = true;
            else if (ch == '{')
                depth++;
            else if (ch == '}')
            {
                depth--;
                if (depth == 0)
                {
                    end = i;
                    return true;
                }
            }
        }

        end = -1;
        return false;
    }

    private static string StripFence(string raw)
    {
        var text = raw.Trim();
        if (!text.StartsWith("```", StringComparison.Ordinal))
            return text;

        var firstNewLine = text.IndexOf('\n');
        var lastFence = text.LastIndexOf("```", StringComparison.Ordinal);
        if (firstNewLine < 0 || lastFence <= firstNewLine)
            return text;

        return text[(firstNewLine + 1)..lastFence].Trim();
    }
}
