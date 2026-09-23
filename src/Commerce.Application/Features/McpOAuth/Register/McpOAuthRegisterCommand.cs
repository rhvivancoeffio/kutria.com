using Commerce.Application.Common.Abstracts;

namespace Commerce.Application.Features.McpOAuth.Register;

public sealed record McpOAuthRegisterCommand(
    string[] RedirectUris,
    string[]? GrantTypes,
    string[]? ResponseTypes,
    string? Scope,
    string? ClientName) : ICommand<McpOAuthRegisterResult>;
