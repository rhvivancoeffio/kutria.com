using Commerce.Application.Common.Abstracts;

namespace Commerce.Application.Features.McpOAuth.GetProtectedResourceMetadata;

public sealed record McpOAuthGetProtectedResourceMetadataQuery(
    string PublicBaseUrl,
    string? FrontendBaseUrl) : IQuery<McpOAuthProtectedResourceMetadata>;

public sealed record McpOAuthProtectedResourceMetadata(
    string Resource,
    string[] AuthorizationServers,
    string[] ScopesSupported,
    string ResourceDocumentation);
