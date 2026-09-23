namespace Commerce.Application.Features.McpOAuth.Register;

public sealed record McpOAuthRegisterResult(
    string ClientId,
    string[] RedirectUris,
    string[] GrantTypes,
    string[] ResponseTypes,
    string? ClientName);
