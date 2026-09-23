using FluentValidation;

namespace Commerce.Application.Features.CreateProduct.ApproveOnboarding;

public sealed class ApproveOnboardingCommandValidator : AbstractValidator<ApproveOnboardingCommand>
{
    public ApproveOnboardingCommandValidator()
    {
        RuleFor(x => x.WorkflowId).NotEmpty();
        RuleFor(x => x.Approvals).NotNull();
        RuleFor(x => x.Approvals.Product)
            .Equal(true)
            .WithMessage("Product approval is required.");
    }
}
