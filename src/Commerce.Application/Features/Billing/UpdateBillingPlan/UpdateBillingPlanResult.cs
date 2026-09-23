namespace Commerce.Application.Features.Billing.UpdateBillingPlan;

public sealed record UpdateBillingPlanResult(Guid Id, string PlanCode, string Status, DateTimeOffset UpdatedAt);
