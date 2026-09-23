using FluentValidation;

namespace Commerce.Application.Features.Generative.StartProductContentByImageGeneration;

public sealed class StartProductContentByImageGenerationCommandValidator
    : AbstractValidator<StartProductContentByImageGenerationCommand>
{
    public StartProductContentByImageGenerationCommandValidator()
    {
        RuleFor(x => x)
            .Must(x => !string.IsNullOrWhiteSpace(x.ImageUrl) || !string.IsNullOrWhiteSpace(x.ImageAttachmentId))
            .WithMessage("ImageUrl or ImageAttachmentId is required.");
        RuleFor(x => x.ImageUrl).MaximumLength(2000).When(x => x.ImageUrl is not null);
        RuleFor(x => x.ImageAttachmentId).MaximumLength(128).When(x => x.ImageAttachmentId is not null);
        RuleFor(x => x.Hint).MaximumLength(500).When(x => x.Hint is not null);
    }
}
