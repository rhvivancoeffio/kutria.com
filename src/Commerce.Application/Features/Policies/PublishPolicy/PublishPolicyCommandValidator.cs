using FluentValidation;

namespace Commerce.Application.Features.Policies.PublishPolicy;

public sealed class PublishPolicyCommandValidator : AbstractValidator<PublishPolicyCommand>
{
    public PublishPolicyCommandValidator()
    {
        RuleFor(x => x.JobId).NotEmpty();
        RuleFor(x => x.EditedText).MaximumLength(200_000);
    }
}
