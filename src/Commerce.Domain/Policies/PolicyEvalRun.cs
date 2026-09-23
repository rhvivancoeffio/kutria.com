using Commerce.Domain.Common;

namespace Commerce.Domain.Policies;

public class PolicyEvalRun : BaseEntity, IWorkspaceScoped
{
    public string TenantId { get; set; } = string.Empty;
    public Guid? WorkspaceId { get; set; }
    public Guid JobId { get; set; }
    public string TemplateVersion { get; set; } = string.Empty;
    public string Status { get; set; } = PolicyEvalRunStatus.Pending;
    public int FailedCount { get; set; }
    public DateTimeOffset? FinishedAt { get; set; }
    public ICollection<PolicyEvalAnswer> Answers { get; set; } = [];
}
