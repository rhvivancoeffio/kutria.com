namespace Commerce.Application.Abstracts;

public interface IMeetGravityHeadlessClient
{
    Task RegisterAsync(MeetGravityRegisterRequest request, CancellationToken cancellationToken = default);

    Task<MeetGravityLoginResult> LoginAsync(
        string email,
        string password,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<MeetGravityOrganization>> ListOrganizationsAsync(
        string accessToken,
        CancellationToken cancellationToken = default);

    Task<MeetGravityClientAppCredentials> CreateOrganizationClientAppAsync(
        string accessToken,
        Guid organizationId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<MeetGravityAvailableProvider>> ListAvailableProvidersAsync(
        string accessToken,
        Guid organizationId,
        CancellationToken cancellationToken = default);

    Task UpdateProviderSettingsAsync(
        string accessToken,
        Guid organizationId,
        Guid providerId,
        MeetGravityUpdateProviderSettingsRequest request,
        CancellationToken cancellationToken = default);
}

public sealed record MeetGravityRegisterRequest(
    string FirstName,
    string LastName,
    string Email,
    string Password,
    string ConfirmPassword,
    string Name,
    string Host,
    int PlatformType,
    string PhoneNumber,
    Guid ProviderId,
    string Language,
    string TimeZone);

public sealed record MeetGravityLoginResult(string AccessToken, string? ExpiresIn, string? TokenType);

public sealed record MeetGravityOrganization(
    Guid OrganizationId,
    string? Name,
    Guid? ProviderId,
    string? ProviderName);

public sealed record MeetGravityClientAppCredentials(string ClientId, string ClientSecret);

public sealed record MeetGravityAvailableProvider(
    Guid ProviderId,
    Guid ProviderTenantId,
    string? Name,
    IReadOnlyList<MeetGravityProviderSetting> ProviderSettings);

public sealed record MeetGravityProviderSetting(
    Guid ProviderSettingId,
    string Key,
    string? Value,
    bool Required,
    string? Label);

public sealed record MeetGravityUpdateProviderSettingsRequest(
    Guid ProviderId,
    Guid ProviderTenantId,
    IReadOnlyList<MeetGravityProviderSettingVariable> Variables);

public sealed record MeetGravityProviderSettingVariable(
    Guid ProviderSettingId,
    string Key,
    string Value);

public static class MeetGravitySettingKeys
{
    public const string Url = "url";
    public const string Email = "meetGravityEmail";
    public const string OrganizationId = "meetGravityOrganizationId";
    public const string ClientId = "meetGravityClientId";
    public const string ClientSecret = "meetGravityClientSecret";
    public const string ProviderId = "meetGravityProviderId";
    public const string ProviderTenantId = "meetGravityProviderTenantId";
    public const string ApiKey = "apiKey";
    /// <summary>Bearer token for native Gravity (MeetGravity login). Not used by GravityAPI.</summary>
    public const string AccessToken = "accessToken";
}

public interface IMeetGravityStoreProvisioner
{
    bool SupportsProvider(string provider);

    /// <summary>
    /// Provisions MeetGravity headless credentials and pushes required provider settings
    /// (overlaying <paramref name="userSettings"/>). Returns keys to merge into SettingsJson.
    /// </summary>
    Task<IReadOnlyDictionary<string, string>> ProvisionAsync(
        string tenantId,
        string tenantName,
        string? ownerDisplayName,
        string provider,
        IReadOnlyDictionary<string, string> userSettings,
        string? existingClientId = null,
        string? existingClientSecret = null,
        CancellationToken cancellationToken = default);
}
