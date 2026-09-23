using FluentValidation;

namespace Commerce.Application.Features.Catalog.Categories.CreateCategory;

public sealed class CreateCategoryCommandValidator : AbstractValidator<CreateCategoryCommand>
{
    public CreateCategoryCommandValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Slug).MaximumLength(200).When(x => x.Slug is not null);
        RuleFor(x => x.Description).MaximumLength(2000).When(x => x.Description is not null);
        RuleFor(x => x.ParentCategoryId).MaximumLength(128).When(x => x.ParentCategoryId is not null);
    }
}
