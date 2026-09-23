using FluentValidation;

namespace Commerce.Application.Features.DataIngestion.ForceEnqueueCatalogVector;

public sealed class ForceEnqueueCatalogVectorCommandValidator : AbstractValidator<ForceEnqueueCatalogVectorCommand>
{
    public ForceEnqueueCatalogVectorCommandValidator()
    {
        RuleFor(x => x.IntegrationId).NotEmpty();
        RuleFor(x => x.ProductId).NotEmpty().MaximumLength(200);
    }
}
