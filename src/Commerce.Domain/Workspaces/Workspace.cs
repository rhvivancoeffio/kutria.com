using Commerce.Domain.Common;

namespace Commerce.Domain.Workspaces;

public class Workspace : BaseEntity, ITenantScoped
{
    public string TenantId { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public bool IsDefault { get; set; }
    public bool IsSystem { get; set; }
    public WorkspaceEnvironment EnvironmentKind { get; set; } = WorkspaceEnvironment.Sandbox;
}
