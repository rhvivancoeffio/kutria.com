using Commerce.Application.Common.Abstracts;

namespace Commerce.Application.Features.McpOAuth.AuthorizeComplete;

public sealed record McpOAuthAuthorizeCompleteCommand(
    string UserId,
    string TenantId,
    string ClientId,
    string RedirectUri,
    string? State,
    string? CodeChallenge,
    string? CodeChallengeMethod,
    string? Scope,
    string? Resource,
    string? ClientName,
    string? PublicBaseUrlOverride) : ICommand<McpOAuthAuthorizeCompleteResult>;
