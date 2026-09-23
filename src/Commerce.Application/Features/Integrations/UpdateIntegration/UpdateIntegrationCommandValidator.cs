using FluentValidation;

namespace Commerce.Application.Features.Integrations.UpdateIntegration;

public sealed class UpdateIntegrationCommandValidator : AbstractValidator<UpdateIntegrationCommand>
{
    public UpdateIntegrationCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.Name).MaximumLength(200).When(x => x.Name is not null);
    }
}
