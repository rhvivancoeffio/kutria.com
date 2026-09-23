using Commerce.Application.Common.Abstracts;

namespace Commerce.Application.Features.McpOAuth.GetProtectedResourceMetadata;

public sealed class McpOAuthGetProtectedResourceMetadataHandler
    : IQueryHandler<McpOAuthGetProtectedResourceMetadataQuery, McpOAuthProtectedResourceMetadata>
{
    public Task<McpOAuthProtectedResourceMetadata> Handle(
        McpOAuthGetProtectedResourceMetadataQuery request,
        CancellationToken cancellationToken)
    {
        var baseUrl = request.PublicBaseUrl.TrimEnd('/');
        var frontend = (request.FrontendBaseUrl ?? "").TrimEnd('/');
        var doc = string.IsNullOrEmpty(frontend) ? $"{baseUrl}/docs" : $"{frontend}/docs";
        return Task.FromResult(new McpOAuthProtectedResourceMetadata(
            baseUrl,
            [baseUrl],
            ["mcp:read", "mcp:write"],
            doc));
    }
}
