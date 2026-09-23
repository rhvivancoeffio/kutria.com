namespace Commerce.Application.Abstracts.Mcp;

public interface IMcpOAuthStore
{
    Task<OAuthClientRecord> RegisterClientAsync(OAuthClientRegistrationRequest request, CancellationToken ct = default);
    Task<OAuthClientRecord?> GetClientAsync(string clientId, CancellationToken ct = default);
    Task SaveAuthorizationCodeAsync(OAuthAuthorizationCodeRecord code, CancellationToken ct = default);
    Task<OAuthAuthorizationCodeRecord?> ConsumeAuthorizationCodeAsync(string code, string clientId, CancellationToken ct = default);
    Task SaveRefreshTokenAsync(OAuthRefreshTokenRecord refreshToken, CancellationToken ct = default);
    Task<OAuthRefreshTokenRecord?> ConsumeRefreshTokenAsync(string refreshToken, string clientId, CancellationToken ct = default);
}

public sealed record OAuthClientRegistrationRequest(
    string[] RedirectUris,
    string[]? GrantTypes = null,
    string[]? ResponseTypes = null,
    string[]? Scope = null,
    string? ClientName = null,
    string? PreferredClientId = null);

public sealed record OAuthClientRecord(
    string ClientId,
    string[] RedirectUris,
    string[] GrantTypes,
    string[] ResponseTypes,
    string? ClientSecret,
    string? ClientName);

public sealed record OAuthAuthorizationCodeRecord(
    string Code,
    string ClientId,
    string RedirectUri,
    string CodeChallenge,
    string CodeChallengeMethod,
    string? Scope,
    string? Resource,
    string UserId,
    string TenantId,
    DateTime ExpiresAt);

public sealed record OAuthRefreshTokenRecord(
    string Token,
    string ClientId,
    string UserId,
    string TenantId,
    string? Scope,
    string? Resource,
    DateTime CreatedAt,
    DateTime ExpiresAt);

public interface IMcpTokenService
{
    string GenerateAccessToken(string userId, string email, string tenantId, string[] scopes, string mcpResourceUri);
}

public interface IAccessTokenValidator
{
    AccessTokenPrincipal? Validate(string token);
}

public sealed record AccessTokenPrincipal(string UserId, string Email, string TenantId, bool AccountOwner);
