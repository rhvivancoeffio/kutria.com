namespace Commerce.Application.Features.ApiKeys.CreateApiKey;

public sealed record CreateApiKeyResult(
    Guid Id,
    string Name,
    string KeyPrefix,
    string Key,
    DateTimeOffset CreatedAt);
