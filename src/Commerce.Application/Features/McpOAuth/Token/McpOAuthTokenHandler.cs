using System.Security.Cryptography;
using System.Text;
using Commerce.Application.Abstracts.Mcp;
using Commerce.Application.Common.Abstracts;
using Commerce.Application.Configuration;
using Commerce.Application.Features.McpOAuth;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Commerce.Application.Features.McpOAuth.Token;

public sealed class McpOAuthTokenHandler(
    IMcpOAuthStore store,
    IMcpTokenService tokenService,
    IOptions<McpOptions> options,
    ILogger<McpOAuthTokenHandler> logger)
    : ICommandHandler<McpOAuthTokenCommand, McpOAuthTokenResult>
{
    public async Task<McpOAuthTokenResult> Handle(McpOAuthTokenCommand request, CancellationToken cancellationToken)
    {
        if (request.GrantType == "authorization_code")
            return await HandleAuthorizationCodeAsync(request, cancellationToken);
        if (request.GrantType == "refresh_token")
            return await HandleRefreshTokenAsync(request, cancellationToken);

        logger.LogWarning("OAuth token unsupported grant_type={GrantType}", request.GrantType);
        throw new McpOAuthException("unsupported_grant_type", "Only authorization_code and refresh_token are supported");
    }

    private async Task<McpOAuthTokenResult> HandleAuthorizationCodeAsync(
        McpOAuthTokenCommand request,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(request.Code) || string.IsNullOrEmpty(request.ClientId) || string.IsNullOrEmpty(request.CodeVerifier))
        {
            logger.LogWarning(
                "OAuth token auth_code missing fields code={HasCode} client_id={HasClient} verifier={HasVerifier}",
                !string.IsNullOrEmpty(request.Code),
                !string.IsNullOrEmpty(request.ClientId),
                !string.IsNullOrEmpty(request.CodeVerifier));
            throw new McpOAuthException("invalid_request", "code, client_id, and code_verifier are required");
        }

        var authCode = await store.ConsumeAuthorizationCodeAsync(request.Code, request.ClientId, cancellationToken);
        if (authCode is null)
        {
            logger.LogWarning(
                "OAuth token auth_code not found or expired client_id={ClientId} code_len={CodeLen}",
                request.ClientId,
                request.Code.Length);
            throw new McpOAuthException("invalid_grant", "Invalid or expired authorization code");
        }

        var computed = ComputeSha256Hash(request.CodeVerifier!);
        if (computed != authCode.CodeChallenge)
        {
            logger.LogWarning(
                "OAuth token PKCE mismatch client_id={ClientId} method={Method}",
                request.ClientId,
                authCode.CodeChallengeMethod);
            throw new McpOAuthException("invalid_grant", "code_verifier does not match");
        }

        var resource = request.Resource ?? authCode.Resource ?? request.PublicBaseUrl.TrimEnd('/');
        var scopes = (authCode.Scope ?? "mcp:read mcp:write").Split(' ', StringSplitOptions.RemoveEmptyEntries);
        var accessToken = tokenService.GenerateAccessToken(authCode.UserId, "", authCode.TenantId, scopes, resource);

        var refreshSeconds = options.Value.RefreshTokenExpirationDays * 24 * 60 * 60;
        var refreshValue = GenerateOpaqueRefreshToken();
        await store.SaveRefreshTokenAsync(new OAuthRefreshTokenRecord(
            Token: refreshValue,
            ClientId: request.ClientId,
            UserId: authCode.UserId,
            TenantId: authCode.TenantId,
            Scope: authCode.Scope,
            Resource: authCode.Resource,
            CreatedAt: DateTime.UtcNow,
            ExpiresAt: DateTime.UtcNow.AddSeconds(refreshSeconds)), cancellationToken);

        logger.LogInformation(
            "OAuth token auth_code exchanged tenant={TenantId} client_id={ClientId} resource={Resource}",
            authCode.TenantId,
            request.ClientId,
            resource);

        return new McpOAuthTokenResult(accessToken, "Bearer", 3600, string.Join(" ", scopes), refreshValue, refreshSeconds);
    }

    private async Task<McpOAuthTokenResult> HandleRefreshTokenAsync(
        McpOAuthTokenCommand request,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(request.RefreshToken) || string.IsNullOrEmpty(request.ClientId))
            throw new McpOAuthException("invalid_request", "refresh_token and client_id are required");

        var consumed = await store.ConsumeRefreshTokenAsync(request.RefreshToken!, request.ClientId, cancellationToken);
        if (consumed is null)
        {
            logger.LogWarning("OAuth token refresh invalid/expired client_id={ClientId}", request.ClientId);
            throw new McpOAuthException("invalid_grant", "Invalid or expired refresh token");
        }

        var resource = request.Resource ?? consumed.Resource ?? request.PublicBaseUrl.TrimEnd('/');
        var scopes = (consumed.Scope ?? "mcp:read mcp:write").Split(' ', StringSplitOptions.RemoveEmptyEntries);
        var accessToken = tokenService.GenerateAccessToken(consumed.UserId, "", consumed.TenantId, scopes, resource);

        var refreshSeconds = options.Value.RefreshTokenExpirationDays * 24 * 60 * 60;
        var refreshValue = GenerateOpaqueRefreshToken();
        await store.SaveRefreshTokenAsync(new OAuthRefreshTokenRecord(
            Token: refreshValue,
            ClientId: request.ClientId,
            UserId: consumed.UserId,
            TenantId: consumed.TenantId,
            Scope: consumed.Scope,
            Resource: consumed.Resource,
            CreatedAt: DateTime.UtcNow,
            ExpiresAt: DateTime.UtcNow.AddSeconds(refreshSeconds)), cancellationToken);

        logger.LogInformation("OAuth token refresh rotated client_id={ClientId}", request.ClientId);
        return new McpOAuthTokenResult(accessToken, "Bearer", 3600, string.Join(" ", scopes), refreshValue, refreshSeconds);
    }

    private static string GenerateOpaqueRefreshToken()
    {
        var bytes = new byte[32];
        RandomNumberGenerator.Fill(bytes);
        return Convert.ToBase64String(bytes).Replace("+", "-").Replace("/", "_").TrimEnd('=');
    }

    private static string ComputeSha256Hash(string input)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(input));
        return Convert.ToBase64String(bytes).Replace("+", "-").Replace("/", "_").TrimEnd('=');
    }
}
