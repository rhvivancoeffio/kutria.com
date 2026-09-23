using FluentValidation;

namespace Commerce.Application.Features.Billing.UpdateBillingPlan;

public sealed class UpdateBillingPlanCommandValidator : AbstractValidator<UpdateBillingPlanCommand>
{
    private static readonly string[] AllowedPlans = ["free", "starter", "pro"];

    public UpdateBillingPlanCommandValidator()
    {
        RuleFor(x => x.PlanCode)
            .NotEmpty()
            .Must(p => AllowedPlans.Contains(p.Trim().ToLowerInvariant()))
            .WithMessage("Plan must be one of: free, starter, pro.");
    }
}
