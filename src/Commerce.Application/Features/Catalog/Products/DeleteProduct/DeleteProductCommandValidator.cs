using FluentValidation;

namespace Commerce.Application.Features.Catalog.Products.DeleteProduct;

public sealed class DeleteProductCommandValidator : AbstractValidator<DeleteProductCommand>
{
    public DeleteProductCommandValidator()
    {
        RuleFor(x => x.ProductId).NotEmpty().MaximumLength(128);
    }
}
