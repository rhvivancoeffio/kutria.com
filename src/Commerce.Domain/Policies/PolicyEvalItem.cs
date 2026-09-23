using Commerce.Domain.Common;

namespace Commerce.Domain.Policies;

public class PolicyEvalItem : BaseEntity, IWorkspaceScoped
{
    public string TenantId { get; set; } = string.Empty;
    public Guid? WorkspaceId { get; set; }
    public string? BaseItemId { get; set; }
    public string Type { get; set; } = string.Empty;
    public string Question { get; set; } = string.Empty;
    public bool IsDisabled { get; set; }
    public int SortOrder { get; set; }
}
