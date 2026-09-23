using System.Security.Cryptography;
using Commerce.Application.Abstracts.Mcp;
using Commerce.Application.Common.Abstracts;
using Commerce.Application.Configuration;
using Commerce.Application.Features.McpOAuth;
using Microsoft.Extensions.Options;

namespace Commerce.Application.Features.McpOAuth.AuthorizeComplete;

public sealed class McpOAuthAuthorizeCompleteHandler(
    IMcpOAuthStore store,
    IOptions<McpOptions> options)
    : ICommandHandler<McpOAuthAuthorizeCompleteCommand, McpOAuthAuthorizeCompleteResult>
{
    public async Task<McpOAuthAuthorizeCompleteResult> Handle(
        McpOAuthAuthorizeCompleteCommand request,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(request.UserId) || string.IsNullOrEmpty(request.TenantId))
            throw new McpOAuthException("invalid_request", "Valid session required. Please sign in first.");

        var opts = options.Value;
        var allowed = GetAllowed(opts);
        var client = await store.GetClientAsync(request.ClientId, cancellationToken);

        if (client is null)
        {
            if (!allowed.Any(p => request.RedirectUri.StartsWith(p, StringComparison.OrdinalIgnoreCase)))
                throw new McpOAuthException("invalid_request", "redirect_uri not allowed");

            client = await store.RegisterClientAsync(new OAuthClientRegistrationRequest(
                RedirectUris: [request.RedirectUri],
                GrantTypes: ["authorization_code"],
                ResponseTypes: ["code"],
                Scope: request.Scope?.Split(' ', StringSplitOptions.RemoveEmptyEntries),
                ClientName: request.ClientName ?? "MCP Client",
                PreferredClientId: request.ClientId), cancellationToken);
        }

        if (!client.RedirectUris.Contains(request.RedirectUri, StringComparer.OrdinalIgnoreCase))
            throw new McpOAuthException("invalid_request", "redirect_uri mismatch");

        var publicBase = !string.IsNullOrEmpty(request.PublicBaseUrlOverride)
            ? request.PublicBaseUrlOverride.TrimEnd('/')
            : opts.PublicBaseUrl.TrimEnd('/');
        var resource = request.Resource ?? publicBase;
        var code = GenerateSecureCode();
        await store.SaveAuthorizationCodeAsync(new OAuthAuthorizationCodeRecord(
            Code: code,
            ClientId: request.ClientId,
            RedirectUri: request.RedirectUri,
            CodeChallenge: request.CodeChallenge ?? "",
            CodeChallengeMethod: request.CodeChallengeMethod ?? "S256",
            Scope: request.Scope,
            Resource: resource,
            UserId: request.UserId,
            TenantId: request.TenantId,
            ExpiresAt: DateTime.UtcNow.AddMinutes(10)), cancellationToken);

        var redirectUrl =
            $"{request.RedirectUri}?code={Uri.EscapeDataString(code)}&state={Uri.EscapeDataString(request.State ?? "")}";
        return new McpOAuthAuthorizeCompleteResult(redirectUrl);
    }

    private static string[] GetAllowed(McpOptions opts) => opts.GetAllowedRedirectUriPrefixes();

    private static string GenerateSecureCode()
    {
        var bytes = new byte[32];
        RandomNumberGenerator.Fill(bytes);
        return Convert.ToBase64String(bytes).Replace("+", "-").Replace("/", "_").TrimEnd('=');
    }
}
