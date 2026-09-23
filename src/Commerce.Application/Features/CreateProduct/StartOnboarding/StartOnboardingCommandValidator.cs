using FluentValidation;

namespace Commerce.Application.Features.CreateProduct.StartOnboarding;

public sealed class StartOnboardingCommandValidator : AbstractValidator<StartOnboardingCommand>
{
    public StartOnboardingCommandValidator()
    {
        RuleFor(x => x)
            .Must(x => !string.IsNullOrWhiteSpace(x.Title)
                       || !string.IsNullOrWhiteSpace(x.ImageUrl)
                       || x.ImageStream is not null)
            .WithMessage("Either title or image is required.");
    }
}
