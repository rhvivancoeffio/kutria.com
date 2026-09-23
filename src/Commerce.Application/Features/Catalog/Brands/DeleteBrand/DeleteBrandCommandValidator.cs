using FluentValidation;

namespace Commerce.Application.Features.Catalog.Brands.DeleteBrand;

public sealed class DeleteBrandCommandValidator : AbstractValidator<DeleteBrandCommand>
{
    public DeleteBrandCommandValidator()
    {
        RuleFor(x => x.BrandId).NotEmpty().MaximumLength(128);
    }
}
