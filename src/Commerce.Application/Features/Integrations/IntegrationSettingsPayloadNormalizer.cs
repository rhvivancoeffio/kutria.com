using System.Text.Json;
using Commerce.Application.Abstracts;

namespace Commerce.Application.Features.Integrations;

/// <summary>
/// Maps JSON request settings to persisted string dictionary (Channels-compatible).
/// </summary>
internal static class IntegrationSettingsPayloadNormalizer
{
    public static Dictionary<string, string> ToStringDictionary(
        IntegrationMetadataDto? meta,
        IReadOnlyDictionary<string, JsonElement>? raw)
    {
        if (raw == null || raw.Count == 0)
            return new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        Dictionary<string, string>? typeByKey = null;
        if (meta?.Settings is { Count: > 0 } list)
        {
            typeByKey = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            foreach (var s in list)
            {
                if (!string.IsNullOrEmpty(s.Key))
                    typeByKey[s.Key] = s.Type ?? "text";
            }
        }

        var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var kv in raw)
        {
            var key = kv.Key;
            var el = kv.Value;
            var isBoolean = typeByKey != null
                && typeByKey.TryGetValue(key, out var t)
                && string.Equals(t, "boolean", StringComparison.OrdinalIgnoreCase);
            result[key] = isBoolean ? BooleanElementToString(el) : DefaultElementToString(el);
        }

        return result;
    }

    private static string BooleanElementToString(JsonElement el)
    {
        object? toConvert = el.ValueKind switch
        {
            JsonValueKind.True => true,
            JsonValueKind.False => false,
            JsonValueKind.String => el.GetString() ?? "",
            JsonValueKind.Number when el.TryGetInt64(out var l) => l,
            JsonValueKind.Number => el.GetDouble(),
            _ => el.GetRawText()
        };
        return Convert.ToBoolean(toConvert).ToString().ToLowerInvariant();
    }

    private static string DefaultElementToString(JsonElement el) =>
        el.ValueKind switch
        {
            JsonValueKind.String => el.GetString() ?? "",
            JsonValueKind.True => "true",
            JsonValueKind.False => "false",
            JsonValueKind.Number => el.GetRawText(),
            JsonValueKind.Null => "",
            _ => el.GetRawText()
        };
}
