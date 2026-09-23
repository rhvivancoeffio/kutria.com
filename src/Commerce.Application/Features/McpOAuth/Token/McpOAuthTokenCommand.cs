using Commerce.Application.Common.Abstracts;

namespace Commerce.Application.Features.McpOAuth.Token;

public sealed record McpOAuthTokenCommand(
    string GrantType,
    string Code,
    string? RedirectUri,
    string ClientId,
    string? CodeVerifier,
    string? Resource,
    string PublicBaseUrl,
    string? RefreshToken) : ICommand<McpOAuthTokenResult>;
