using FluentValidation;

namespace Commerce.Application.Features.Brains.UploadBrainDocument;

public sealed class UploadBrainDocumentCommandValidator : AbstractValidator<UploadBrainDocumentCommand>
{
    public const int MaxBytes = 20_000_000;

    public UploadBrainDocumentCommandValidator()
    {
        RuleFor(x => x.BrainKey).NotEmpty().MaximumLength(64);
        RuleFor(x => x.FileName).NotEmpty().MaximumLength(260);
        RuleFor(x => x.Content).NotEmpty().Must(content => content.Length <= MaxBytes)
            .WithMessage($"File must be {MaxBytes} bytes or smaller.");
    }
}
