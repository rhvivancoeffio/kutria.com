using FluentValidation;

namespace Commerce.Application.Features.Generative.StartProductVideoGeneration;

public sealed class StartProductVideoGenerationCommandValidator
    : AbstractValidator<StartProductVideoGenerationCommand>
{
    public StartProductVideoGenerationCommandValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Description).MaximumLength(8000).When(x => x.Description is not null);
        RuleFor(x => x.ImageUrl).MaximumLength(2000).When(x => x.ImageUrl is not null);
    }
}
