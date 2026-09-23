using FluentValidation;
using Commerce.Application.Abstracts;

namespace Commerce.Application.Features.DataIngestion.EnqueueDataIngest;

public sealed class EnqueueDataIngestCommandValidator : AbstractValidator<EnqueueDataIngestCommand>
{
    public EnqueueDataIngestCommandValidator()
    {
        RuleFor(x => x.IntegrationId).NotEmpty();
        RuleFor(x => x.Kind)
            .Must(kind =>
                string.IsNullOrWhiteSpace(kind)
                || string.Equals(kind, DataIngestKinds.Catalog, StringComparison.OrdinalIgnoreCase)
                || string.Equals(kind, DataIngestKinds.Orders, StringComparison.OrdinalIgnoreCase)
                || string.Equals(kind, DataIngestKinds.All, StringComparison.OrdinalIgnoreCase))
            .WithMessage("Kind must be catalog, orders, or all.");
    }
}
