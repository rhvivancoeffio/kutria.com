using Commerce.Domain.Common;

namespace Commerce.Domain.Integrations;

/// <summary>
/// Tenant-scoped connection. Optional WorkspaceId partitions Sandbox vs Production.
/// </summary>
public class Integration : BaseEntity, IWorkspaceScoped
{
    public string TenantId { get; set; } = string.Empty;
    public Guid? WorkspaceId { get; set; }
    public string Provider { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string SettingsJson { get; set; } = "{}";
    public bool IsActive { get; set; } = true;
}
