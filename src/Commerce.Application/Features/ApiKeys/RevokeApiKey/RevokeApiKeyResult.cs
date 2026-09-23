namespace Commerce.Application.Features.ApiKeys.RevokeApiKey;

public sealed record RevokeApiKeyResult(Guid Id, bool IsActive);
