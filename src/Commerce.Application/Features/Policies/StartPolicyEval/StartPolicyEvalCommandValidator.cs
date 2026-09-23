using FluentValidation;

namespace Commerce.Application.Features.Policies.StartPolicyEval;

public sealed class StartPolicyEvalCommandValidator : AbstractValidator<StartPolicyEvalCommand>
{
    public StartPolicyEvalCommandValidator()
    {
        RuleFor(x => x.JobId).NotEmpty();
    }
}
