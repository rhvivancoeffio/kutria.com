using FluentValidation;

namespace Commerce.Application.Features.Catalog.Attributes.DeleteAttribute;

public sealed class DeleteAttributeCommandValidator : AbstractValidator<DeleteAttributeCommand>
{
    public DeleteAttributeCommandValidator()
    {
        RuleFor(x => x.EntityAttributeId).NotEmpty().MaximumLength(128);
    }
}
