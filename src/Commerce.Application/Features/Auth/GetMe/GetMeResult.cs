namespace Commerce.Application.Features.Auth.GetMe;

public sealed record GetMeResult(
    Guid UserId,
    string Email,
    string DisplayName,
    DateTimeOffset CreatedAt,
    bool HasPassword,
    bool IsAccountOwner,
    string TenantId,
    string CommerceStoreMode);
