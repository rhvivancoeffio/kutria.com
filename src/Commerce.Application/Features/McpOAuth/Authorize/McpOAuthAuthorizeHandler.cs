using Commerce.Application.Abstracts.Mcp;
using Commerce.Application.Common.Abstracts;
using Commerce.Application.Configuration;
using Commerce.Application.Features.McpOAuth;
using Microsoft.Extensions.Options;

namespace Commerce.Application.Features.McpOAuth.Authorize;

public sealed class McpOAuthAuthorizeHandler(
    IMcpOAuthStore store,
    IOptions<McpOptions> options)
    : IQueryHandler<McpOAuthAuthorizeQuery, McpOAuthAuthorizeResult>
{
    public async Task<McpOAuthAuthorizeResult> Handle(McpOAuthAuthorizeQuery request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(request.ClientId) || string.IsNullOrEmpty(request.RedirectUri))
            throw new McpOAuthException("invalid_request", "client_id and redirect_uri are required");

        var opts = options.Value;
        var allowed = GetAllowed(opts);
        if (!allowed.Any(p => request.RedirectUri.StartsWith(p, StringComparison.OrdinalIgnoreCase)))
            throw new McpOAuthException("invalid_request", "redirect_uri not allowed");

        var client = await store.GetClientAsync(request.ClientId, cancellationToken);
        var frontendUrl = opts.FrontendBaseUrl.TrimEnd('/');
        var query = new Dictionary<string, string?>
        {
            ["client_id"] = request.ClientId,
            ["redirect_uri"] = request.RedirectUri,
            ["response_type"] = request.ResponseType ?? "code",
            ["scope"] = request.Scope,
            ["state"] = request.State,
            ["code_challenge"] = request.CodeChallenge,
            ["code_challenge_method"] = request.CodeChallengeMethod ?? "S256",
            ["resource"] = request.Resource,
            ["client_name"] = client?.ClientName
        };
        var qs = string.Join("&", query.Where(kv => !string.IsNullOrEmpty(kv.Value))
            .Select(kv => $"{Uri.EscapeDataString(kv.Key)}={Uri.EscapeDataString(kv.Value!)}"));
        return new McpOAuthAuthorizeResult($"{frontendUrl}/mcp/oauth/consent?{qs}");
    }

    private static string[] GetAllowed(McpOptions opts) => opts.GetAllowedRedirectUriPrefixes();
}
