using System.Text.Json;

namespace Commerce.Infrastructure.Agents;

internal static class WorkflowPayload
{
    public static string ReadOrderId(string payload)
        => ReadString(payload, "order_id", "orderId") ?? "unknown";

    public static string? ReadPaymentId(string payload)
        => ReadString(payload, "payment_id", "paymentId");

    public static string Json(IReadOnlyDictionary<string, object?> values)
        => JsonSerializer.Serialize(values);

    private static string? ReadString(string payload, params string[] names)
    {
        try
        {
            using var document = JsonDocument.Parse(payload);
            foreach (var name in names)
            {
                if (!document.RootElement.TryGetProperty(name, out var value)
                    || value.ValueKind != JsonValueKind.String)
                {
                    continue;
                }

                var text = value.GetString();
                if (!string.IsNullOrWhiteSpace(text))
                {
                    return text;
                }
            }
        }
        catch (JsonException)
        {
            // Payload is not JSON. Callers fall back to a placeholder id.
        }

        return null;
    }
}
