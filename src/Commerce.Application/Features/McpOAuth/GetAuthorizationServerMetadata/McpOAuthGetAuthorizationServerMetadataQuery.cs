using Commerce.Application.Common.Abstracts;

namespace Commerce.Application.Features.McpOAuth.GetAuthorizationServerMetadata;

public sealed record McpOAuthGetAuthorizationServerMetadataQuery(string PublicBaseUrl)
    : IQuery<McpOAuthAuthorizationServerMetadata>;

public sealed record McpOAuthAuthorizationServerMetadata(
    string Issuer,
    string AuthorizationEndpoint,
    string TokenEndpoint,
    string RegistrationEndpoint,
    string[] CodeChallengeMethodsSupported,
    string[] ScopesSupported,
    string[] ResponseTypesSupported,
    string[] GrantTypesSupported);
