using Commerce.Application.Abstracts.Mcp;
using Commerce.Application.Common.Abstracts;
using Commerce.Application.Configuration;
using Commerce.Application.Features.McpOAuth;
using Microsoft.Extensions.Options;

namespace Commerce.Application.Features.McpOAuth.Register;

public sealed class McpOAuthRegisterHandler(
    IMcpOAuthStore store,
    IOptions<McpOptions> options)
    : ICommandHandler<McpOAuthRegisterCommand, McpOAuthRegisterResult>
{
    public async Task<McpOAuthRegisterResult> Handle(McpOAuthRegisterCommand request, CancellationToken cancellationToken)
    {
        if (request.RedirectUris is null || request.RedirectUris.Length == 0)
            throw new McpOAuthException("invalid_request", "redirect_uris is required");

        var allowed = options.Value.GetAllowedRedirectUriPrefixes();

        foreach (var uri in request.RedirectUris)
        {
            if (!allowed.Any(p => uri.StartsWith(p, StringComparison.OrdinalIgnoreCase)))
                throw new McpOAuthException("invalid_request", $"redirect_uri not allowed: {uri}");
        }

        var client = await store.RegisterClientAsync(new OAuthClientRegistrationRequest(
            RedirectUris: request.RedirectUris,
            GrantTypes: request.GrantTypes ?? ["authorization_code"],
            ResponseTypes: request.ResponseTypes ?? ["code"],
            Scope: request.Scope?.Split(' ', StringSplitOptions.RemoveEmptyEntries),
            ClientName: request.ClientName), cancellationToken);

        return new McpOAuthRegisterResult(
            client.ClientId,
            client.RedirectUris,
            client.GrantTypes,
            client.ResponseTypes,
            client.ClientName);
    }
}
