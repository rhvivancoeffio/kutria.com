using Commerce.Domain.Common;

namespace Commerce.Domain.Agents;

public class Proposal : BaseEntity, ITenantScoped
{
    public const string Pending = "pending";
    public const string Approved = "approved";
    public const string Executed = "executed";

    public string TenantId { get; set; } = string.Empty;
    public string AgentKey { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Action { get; set; } = string.Empty;
    public string Impact { get; set; } = string.Empty;
    public string Status { get; set; } = Pending;
    public DateTimeOffset? ApprovedAt { get; set; }
    public DateTimeOffset? ExecutedAt { get; set; }
}
