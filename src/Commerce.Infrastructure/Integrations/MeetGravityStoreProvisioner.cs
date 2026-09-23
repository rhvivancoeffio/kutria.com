using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Commerce.Application.Abstracts;

namespace Commerce.Infrastructure.Integrations;

public sealed class MeetGravityStoreProvisioner(
    IMeetGravityHeadlessClient client,
    IOptions<MeetGravityOptions> options,
    ILogger<MeetGravityStoreProvisioner> logger) : IMeetGravityStoreProvisioner
{
    public bool SupportsProvider(string provider)
    {
        var ids = options.Value.ProviderIds;
        return !string.IsNullOrWhiteSpace(provider)
            && ids.Keys.Any(k => string.Equals(k, provider, StringComparison.OrdinalIgnoreCase));
    }

    public async Task<IReadOnlyDictionary<string, string>> ProvisionAsync(
        string tenantId,
        string tenantName,
        string? ownerDisplayName,
        string provider,
        IReadOnlyDictionary<string, string> userSettings,
        string? existingClientId = null,
        string? existingClientSecret = null,
        CancellationToken cancellationToken = default)
    {
        var opts = options.Value;
        if (!TryResolveProviderId(provider, out var providerId))
            throw new InvalidOperationException($"MeetGravity providerId is not configured for '{provider}'.");

        var id = $"{Guid.NewGuid()}";
        var email = $"{id}@kutria.com";
        var password = id;
        var (firstName, lastName) = SplitName(ownerDisplayName, tenantName, tenantId);
        var orgName = string.IsNullOrWhiteSpace(tenantName) ? tenantId : tenantName.Trim();
        var host = BuildStableHost(id);

        try
        {
            await client.RegisterAsync(
                new MeetGravityRegisterRequest(
                    firstName,
                    lastName,
                    email,
                    password,
                    password,
                    orgName,
                    host,
                    opts.PlatformType,
                    opts.DefaultPhoneNumber,
                    providerId,
                    opts.DefaultLanguage,
                    opts.DefaultTimeZone),
                cancellationToken);
        }
        catch (MeetGravityApiException ex) when (ex.StatusCode is 400 or 409 or 422)
        {
            logger.LogInformation(
                "MeetGravity register indicated existing account for {Email}; continuing with login.",
                email);
        }

        var login = await client.LoginAsync(email, password, cancellationToken);
        var orgs = await client.ListOrganizationsAsync(login.AccessToken, cancellationToken);
        if (orgs.Count == 0)
            throw new InvalidOperationException("MeetGravity returned no organizations after login.");

        var org = orgs[0];

        string clientId;
        string clientSecret;
        if (!string.IsNullOrWhiteSpace(existingClientId) && !string.IsNullOrWhiteSpace(existingClientSecret))
        {
            clientId = existingClientId;
            clientSecret = existingClientSecret;
            logger.LogInformation(
                "Reusing MeetGravity client app {ClientId} for tenant {TenantId} provider {Provider}.",
                clientId,
                tenantId,
                provider);
        }
        else
        {
            var creds = await client.CreateOrganizationClientAppAsync(
                login.AccessToken,
                org.OrganizationId,
                cancellationToken);
            clientId = creds.ClientId;
            clientSecret = creds.ClientSecret;
        }

        var baseUrl = opts.BaseUrl.Trim().TrimEnd('/');
        var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            [MeetGravitySettingKeys.Url] = baseUrl,
            [MeetGravitySettingKeys.Email] = email,
            [MeetGravitySettingKeys.OrganizationId] = org.OrganizationId.ToString(),
            [MeetGravitySettingKeys.ClientId] = clientId,
            [MeetGravitySettingKeys.ClientSecret] = clientSecret,
            [MeetGravitySettingKeys.ProviderId] = providerId.ToString(),
            [MeetGravitySettingKeys.ApiKey] = clientSecret
        };

        // Gravity: stop after ClientApp — no Providers/availables or settings PUT.
        // Store API auth uses MeetGravity login access_token (Bearer), not X-Gravity-API-Key.
        if (string.Equals(provider, "Gravity", StringComparison.OrdinalIgnoreCase))
        {
            result[MeetGravitySettingKeys.AccessToken] = login.AccessToken;
            logger.LogInformation(
                "MeetGravity Gravity provider: skipping Providers/availables and settings sync for tenant {TenantId}.",
                tenantId);
            return result;
        }

        var available = await client.ListAvailableProvidersAsync(
            login.AccessToken,
            org.OrganizationId,
            cancellationToken);
        var store = available.FirstOrDefault(p => p.ProviderId == providerId)
            ?? available.FirstOrDefault()
            ?? throw new InvalidOperationException("MeetGravity returned no available providers.");

        var variables = BuildRequiredVariables(store, userSettings);
        await client.UpdateProviderSettingsAsync(
            login.AccessToken,
            org.OrganizationId,
            store.ProviderId,
            new MeetGravityUpdateProviderSettingsRequest(
                store.ProviderId,
                store.ProviderTenantId,
                variables),
            cancellationToken);

        result[MeetGravitySettingKeys.ProviderId] = store.ProviderId.ToString();
        result[MeetGravitySettingKeys.ProviderTenantId] = store.ProviderTenantId.ToString();
        return result;
    }

    internal static IReadOnlyList<MeetGravityProviderSettingVariable> BuildRequiredVariables(
        MeetGravityAvailableProvider store,
        IReadOnlyDictionary<string, string> userSettings)
    {
        var required = store.ProviderSettings
            .Where(s => s.Required && !string.IsNullOrWhiteSpace(s.Key) && s.ProviderSettingId != Guid.Empty)
            .ToList();

        if (required.Count == 0)
            throw new InvalidOperationException(
                $"MeetGravity provider '{store.Name ?? store.ProviderId.ToString()}' has no required settings.");

        var lookup = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var kv in userSettings)
        {
            if (!string.IsNullOrWhiteSpace(kv.Key) && kv.Value is not null)
                lookup[kv.Key] = kv.Value;
        }

        var variables = new List<MeetGravityProviderSettingVariable>(required.Count);
        foreach (var setting in required)
        {
            var value = ResolveSettingValue(setting, lookup);
            variables.Add(new MeetGravityProviderSettingVariable(
                setting.ProviderSettingId,
                setting.Key,
                value));
        }

        return variables;
    }

    private static string ResolveSettingValue(
        MeetGravityProviderSetting setting,
        IReadOnlyDictionary<string, string> userSettings)
    {
        if (userSettings.TryGetValue(setting.Key, out var fromUser) && !string.IsNullOrWhiteSpace(fromUser))
            return fromUser;

        if (!string.IsNullOrWhiteSpace(setting.Value))
            return setting.Value;

        return string.Empty;
    }

    private bool TryResolveProviderId(string provider, out Guid providerId)
    {
        providerId = Guid.Empty;
        foreach (var kv in options.Value.ProviderIds)
        {
            if (!string.Equals(kv.Key, provider, StringComparison.OrdinalIgnoreCase))
                continue;
            return Guid.TryParse(kv.Value, out providerId);
        }

        return false;
    }

    private static (string FirstName, string LastName) SplitName(
        string? ownerDisplayName,
        string tenantName,
        string tenantId)
    {
        var source = !string.IsNullOrWhiteSpace(ownerDisplayName)
            ? ownerDisplayName.Trim()
            : !string.IsNullOrWhiteSpace(tenantName)
                ? tenantName.Trim()
                : tenantId;

        var parts = source.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (parts.Length == 0)
            return ("Tenant", "Owner");
        if (parts.Length == 1)
            return (parts[0], "Owner");
        return (parts[0], string.Join(' ', parts.Skip(1)));
    }

    private static string BuildStableHost(string tenantId)
    {
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(tenantId));
        var value = BitConverter.ToUInt32(hash, 0) % 100_000_000u;
        return value.ToString("D8", CultureInfo.InvariantCulture);
    }
}
