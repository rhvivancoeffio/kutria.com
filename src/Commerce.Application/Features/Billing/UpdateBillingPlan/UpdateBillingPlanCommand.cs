using Commerce.Application.Common.Abstracts;

namespace Commerce.Application.Features.Billing.UpdateBillingPlan;

public sealed record UpdateBillingPlanCommand(string PlanCode) : ICommand<UpdateBillingPlanResult>;
