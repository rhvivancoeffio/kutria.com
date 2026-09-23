using FluentValidation;

namespace Commerce.Application.Features.Catalog.Attributes.UpdateAttribute;

public sealed class UpdateAttributeCommandValidator : AbstractValidator<UpdateAttributeCommand>
{
    public UpdateAttributeCommandValidator()
    {
        RuleFor(x => x.EntityAttributeId).NotEmpty().MaximumLength(128);
        RuleFor(x => x.EntityName).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.SpecificationType).InclusiveBetween(1, 8);
        RuleFor(x => x.Description).MaximumLength(2000).When(x => x.Description is not null);
        RuleFor(x => x.Label).MaximumLength(200).When(x => x.Label is not null);
        RuleFor(x => x.Values).MaximumLength(4000).When(x => x.Values is not null);
        RuleFor(x => x.SectionGroup).MaximumLength(200).When(x => x.SectionGroup is not null);
    }
}
