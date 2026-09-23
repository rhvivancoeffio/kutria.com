using FluentValidation;

namespace Commerce.Application.Features.Catalog.Products.UpdateProduct;

public sealed class UpdateProductCommandValidator : AbstractValidator<UpdateProductCommand>
{
    public UpdateProductCommandValidator()
    {
        RuleFor(x => x.ProductId).NotEmpty().MaximumLength(128);
        RuleFor(x => x.Name).NotEmpty().MaximumLength(500);
        RuleFor(x => x.Description).NotEmpty().MaximumLength(8000);
        RuleFor(x => x.BrandId).MaximumLength(128).When(x => x.BrandId is not null);
        RuleFor(x => x.BrandName).MaximumLength(200).When(x => x.BrandName is not null);
        RuleFor(x => x.CategoryId).MaximumLength(128).When(x => x.CategoryId is not null);
        RuleFor(x => x.CategoryPath).MaximumLength(500).When(x => x.CategoryPath is not null);
        RuleFor(x => x.ImageUrl).MaximumLength(2000).When(x => x.ImageUrl is not null);
    }
}
