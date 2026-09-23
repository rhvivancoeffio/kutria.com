using FluentValidation;

namespace Commerce.Application.Features.Catalog.Categories.DeleteCategory;

public sealed class DeleteCategoryCommandValidator : AbstractValidator<DeleteCategoryCommand>
{
    public DeleteCategoryCommandValidator()
    {
        RuleFor(x => x.CategoryId).NotEmpty().MaximumLength(128);
    }
}
