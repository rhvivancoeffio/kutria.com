using FluentValidation;

namespace Commerce.Application.Features.Generative.StartProductContentGeneration;

public sealed class StartProductContentGenerationCommandValidator
    : AbstractValidator<StartProductContentGenerationCommand>
{
    private static readonly HashSet<string> AllowedModes = new(StringComparer.OrdinalIgnoreCase)
    {
        "all", "description", "bullets", "seo", "name"
    };

    public StartProductContentGenerationCommandValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleForEach(x => x.Modes!)
            .Must(m => AllowedModes.Contains(m))
            .When(x => x.Modes is { Count: > 0 })
            .WithMessage("Invalid mode.");
    }
}
