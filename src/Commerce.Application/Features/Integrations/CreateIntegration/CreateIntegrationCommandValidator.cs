using FluentValidation;

namespace Commerce.Application.Features.Integrations.CreateIntegration;

public sealed class CreateIntegrationCommandValidator : AbstractValidator<CreateIntegrationCommand>
{
    public CreateIntegrationCommandValidator()
    {
        RuleFor(x => x.Provider).NotEmpty().MaximumLength(64);
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
    }
}
