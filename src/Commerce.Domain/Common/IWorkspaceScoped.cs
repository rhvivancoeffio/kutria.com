namespace Commerce.Domain.Common;

/// <summary>
/// Optional partition inside a Finbuckle tenant. Never replaces TenantId isolation.
/// </summary>
public interface IWorkspaceScoped : ITenantScoped
{
    Guid? WorkspaceId { get; set; }
}
