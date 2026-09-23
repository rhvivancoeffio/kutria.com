namespace Commerce.Application.Features.ApiKeys.ListApiKeys;

public sealed record ListApiKeysResult(IReadOnlyList<ApiKeyListItem> Items);

public sealed record ApiKeyListItem(
    Guid Id,
    string Name,
    string KeyPrefix,
    DateTimeOffset CreatedAt,
    DateTimeOffset? LastUsedAt,
    bool IsActive);
