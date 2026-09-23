using Commerce.Application.Common.Abstracts;

namespace Commerce.Application.Features.McpOAuth.Authorize;

public sealed record McpOAuthAuthorizeQuery(
    string ClientId,
    string RedirectUri,
    string? ResponseType,
    string? Scope,
    string? State,
    string? CodeChallenge,
    string? CodeChallengeMethod,
    string? Resource) : IQuery<McpOAuthAuthorizeResult>;
