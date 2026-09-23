using FluentValidation;

namespace Commerce.Application.Features.Brains.PublishBrainDocument;

public sealed class PublishBrainDocumentCommandValidator : AbstractValidator<PublishBrainDocumentCommand>
{
    public PublishBrainDocumentCommandValidator()
    {
        RuleFor(x => x.JobId).NotEmpty();
        RuleFor(x => x.EditedText).MaximumLength(200_000);
    }
}
