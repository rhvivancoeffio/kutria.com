using FluentValidation;

namespace Commerce.Application.Features.Catalog.Brands.CreateBrand;

public sealed class CreateBrandCommandValidator : AbstractValidator<CreateBrandCommand>
{
    public CreateBrandCommandValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Description).MaximumLength(2000).When(x => x.Description is not null);
        RuleFor(x => x.ImageUrl).MaximumLength(2000).When(x => x.ImageUrl is not null);
    }
}
