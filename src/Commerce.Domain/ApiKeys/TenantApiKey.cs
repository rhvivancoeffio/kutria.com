using Commerce.Domain.Common;

namespace Commerce.Domain.ApiKeys;

public class TenantApiKey : BaseEntity, IWorkspaceScoped
{
    public string TenantId { get; set; } = string.Empty;
    public Guid? WorkspaceId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string KeyPrefix { get; set; } = string.Empty;
    public string KeyHash { get; set; } = string.Empty;
    public DateTimeOffset? LastUsedAt { get; set; }
    public DateTimeOffset? RevokedAt { get; set; }
    public bool IsActive { get; set; } = true;
}
