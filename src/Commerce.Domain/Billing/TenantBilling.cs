using Commerce.Domain.Common;

namespace Commerce.Domain.Billing;

/// <summary>
/// Billing root is the Finbuckle tenant (not Workspace).
/// </summary>
public class TenantBilling : BaseEntity, ITenantScoped
{
    public string TenantId { get; set; } = string.Empty;
    public string PlanCode { get; set; } = "free";
    public BillingStatus Status { get; set; } = BillingStatus.Trialing;
    public string? StripeCustomerId { get; set; }
    public string? StripeSubscriptionId { get; set; }
    public DateTimeOffset? TrialEndsAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;
}
