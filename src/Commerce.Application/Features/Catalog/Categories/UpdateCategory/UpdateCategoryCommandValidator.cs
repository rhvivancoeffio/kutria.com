using FluentValidation;

namespace Commerce.Application.Features.Catalog.Categories.UpdateCategory;

public sealed class UpdateCategoryCommandValidator : AbstractValidator<UpdateCategoryCommand>
{
    public UpdateCategoryCommandValidator()
    {
        RuleFor(x => x.CategoryId).NotEmpty().MaximumLength(128);
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Slug).MaximumLength(200).When(x => x.Slug is not null);
        RuleFor(x => x.Description).MaximumLength(2000).When(x => x.Description is not null);
        RuleFor(x => x.ParentCategoryId).MaximumLength(128).When(x => x.ParentCategoryId is not null);
        RuleFor(x => x.ParentCategoryId)
            .Must((cmd, parent) => parent is null || !string.Equals(parent, cmd.CategoryId, StringComparison.OrdinalIgnoreCase))
            .WithMessage("A category cannot be its own parent.");
    }
}
