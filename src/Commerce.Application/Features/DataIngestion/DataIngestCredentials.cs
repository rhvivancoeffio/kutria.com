using System.Text.Json;
using Commerce.Application.Abstracts;

namespace Commerce.Application.Features.DataIngestion;

public static class DataIngestCredentials
{
    public const string GravityApiKeyHeader = "X-Gravity-API-Key";
    public const string GravityOrgIdHeader = "x-Org-Id";
    public const string GravityCheckoutProviderTenantIdHeader = "x-CheckOut-Provider-Tenant-Id";
    /// <summary>Gravity cart/checkout expects this tenant id on store API calls.</summary>
    public const string GravityCheckoutProviderTenantId = "00000000-0000-0000-0000-000000000000";
    public const string OrganizationIdSettingKey = "organizationId";
    public const string SellerIdSettingKey = "sellerId";
    public const string SellerNameSettingKey = "sellerName";

    public static bool TryParse(string? settingsJson, out GravityStoreCredentials credentials)
        => TryParse(settingsJson, out credentials, out _);

    public static bool TryParse(
        string? settingsJson,
        out GravityStoreCredentials credentials,
        out string failureReason)
    {
        credentials = null!;
        failureReason = string.Empty;

        if (string.IsNullOrWhiteSpace(settingsJson))
        {
            failureReason = "SettingsJson is empty.";
            return false;
        }

        Dictionary<string, string> map;
        try
        {
            using var doc = JsonDocument.Parse(settingsJson);
            if (doc.RootElement.ValueKind != JsonValueKind.Object)
            {
                failureReason = $"SettingsJson root is {doc.RootElement.ValueKind}, expected Object.";
                return false;
            }

            map = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            foreach (var prop in doc.RootElement.EnumerateObject())
            {
                var value = prop.Value.ValueKind switch
                {
                    JsonValueKind.String => prop.Value.GetString() ?? string.Empty,
                    JsonValueKind.Number => prop.Value.GetRawText(),
                    JsonValueKind.True => "true",
                    JsonValueKind.False => "false",
                    JsonValueKind.Null => string.Empty,
                    _ => prop.Value.GetRawText()
                };
                map[prop.Name] = value;
            }
        }
        catch (JsonException ex)
        {
            failureReason = $"SettingsJson is not valid JSON: {ex.Message}";
            return false;
        }

        var keys = string.Join(", ", map.Keys.OrderBy(k => k, StringComparer.OrdinalIgnoreCase));

        if (!TryGet(map, MeetGravitySettingKeys.Url, out var url)
            && !TryGet(map, "baseUrl", out url))
        {
            failureReason = $"Missing url/baseUrl. Keys present: [{keys}].";
            return false;
        }

        TryGet(map, MeetGravitySettingKeys.AccessToken, out var accessToken);
        if (string.IsNullOrWhiteSpace(accessToken))
            TryGet(map, "access_token", out accessToken);

        var hasBearer = !string.IsNullOrWhiteSpace(accessToken) && !LooksLikeUiPlaceholder(accessToken);

        string apiKey = string.Empty;
        if (!TryGet(map, MeetGravitySettingKeys.ApiKey, out apiKey)
            && !TryGet(map, MeetGravitySettingKeys.ClientSecret, out apiKey))
        {
            apiKey = string.Empty;
        }

        // Prefer MeetGravity client secret when apiKey looks like UI placeholder text.
        if (!string.IsNullOrWhiteSpace(apiKey)
            && LooksLikeUiPlaceholder(apiKey)
            && TryGet(map, MeetGravitySettingKeys.ClientSecret, out var clientSecret)
            && !LooksLikeUiPlaceholder(clientSecret))
        {
            apiKey = clientSecret;
        }

        var hasApiKey = !string.IsNullOrWhiteSpace(apiKey) && !LooksLikeUiPlaceholder(apiKey);

        // Gravity (native): accessToken → Bearer. GravityAPI: apiKey → X-Gravity-API-Key.
        if (!hasBearer && !hasApiKey)
        {
            failureReason =
                $"Missing accessToken (Gravity) or apiKey/meetGravityClientSecret (GravityAPI). Keys present: [{keys}].";
            return false;
        }

        if (!TryGet(map, OrganizationIdSettingKey, out var organizationId)
            && !TryGet(map, MeetGravitySettingKeys.OrganizationId, out organizationId))
        {
            failureReason =
                $"Missing organizationId/{MeetGravitySettingKeys.OrganizationId}. Keys present: [{keys}].";
            return false;
        }

        TryGet(map, SellerIdSettingKey, out var sellerId);

        credentials = new GravityStoreCredentials(
            url.Trim().TrimEnd('/'),
            hasApiKey ? apiKey.Trim() : string.Empty,
            organizationId.Trim(),
            string.IsNullOrWhiteSpace(sellerId) ? null : sellerId.Trim(),
            hasBearer ? accessToken.Trim() : null);
        return true;
    }

    private static bool LooksLikeUiPlaceholder(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return true;
        if (value.Contains(' ', StringComparison.Ordinal))
            return true;
        if (value.Contains("integraciones", StringComparison.OrdinalIgnoreCase))
            return true;
        return false;
    }

    public static string MaskApiKey(string? apiKey)
    {
        if (string.IsNullOrWhiteSpace(apiKey))
            return "(empty)";
        if (apiKey.Length <= 8)
            return "***";
        return $"{apiKey[..4]}…{apiKey[^4..]} (len={apiKey.Length})";
    }

    private static bool TryGet(Dictionary<string, string> map, string key, out string value)
    {
        if (map.TryGetValue(key, out var raw) && !string.IsNullOrWhiteSpace(raw))
        {
            value = raw;
            return true;
        }

        value = string.Empty;
        return false;
    }
}
