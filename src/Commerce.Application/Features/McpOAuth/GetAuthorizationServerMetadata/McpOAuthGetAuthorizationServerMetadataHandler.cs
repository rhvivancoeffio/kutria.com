using Commerce.Application.Common.Abstracts;

namespace Commerce.Application.Features.McpOAuth.GetAuthorizationServerMetadata;

public sealed class McpOAuthGetAuthorizationServerMetadataHandler
    : IQueryHandler<McpOAuthGetAuthorizationServerMetadataQuery, McpOAuthAuthorizationServerMetadata>
{
    public Task<McpOAuthAuthorizationServerMetadata> Handle(
        McpOAuthGetAuthorizationServerMetadataQuery request,
        CancellationToken cancellationToken)
    {
        var baseUrl = request.PublicBaseUrl.TrimEnd('/');
        return Task.FromResult(new McpOAuthAuthorizationServerMetadata(
            Issuer: baseUrl,
            AuthorizationEndpoint: $"{baseUrl}/oauth/authorize",
            TokenEndpoint: $"{baseUrl}/oauth/token",
            RegistrationEndpoint: $"{baseUrl}/oauth/register",
            CodeChallengeMethodsSupported: ["S256"],
            ScopesSupported: ["mcp:read", "mcp:write"],
            ResponseTypesSupported: ["code"],
            GrantTypesSupported: ["authorization_code", "refresh_token"]));
    }
}
