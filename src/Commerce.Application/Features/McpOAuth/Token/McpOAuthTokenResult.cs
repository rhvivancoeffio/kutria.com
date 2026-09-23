namespace Commerce.Application.Features.McpOAuth.Token;

public sealed record McpOAuthTokenResult(
    string AccessToken,
    string TokenType,
    int ExpiresIn,
    string Scope,
    string? RefreshToken = null,
    int? RefreshTokenExpiresIn = null);
