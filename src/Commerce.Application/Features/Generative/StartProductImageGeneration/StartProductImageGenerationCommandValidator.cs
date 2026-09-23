using FluentValidation;

namespace Commerce.Application.Features.Generative.StartProductImageGeneration;

public sealed class StartProductImageGenerationCommandValidator
    : AbstractValidator<StartProductImageGenerationCommand>
{
    public StartProductImageGenerationCommandValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Description).MaximumLength(8000).When(x => x.Description is not null);
        RuleFor(x => x.Count).InclusiveBetween(1, 2);
    }
}
