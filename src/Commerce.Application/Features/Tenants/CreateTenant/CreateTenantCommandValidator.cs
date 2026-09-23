using FluentValidation;

namespace Commerce.Application.Features.Tenants.CreateTenant;

public sealed class CreateTenantCommandValidator : AbstractValidator<CreateTenantCommand>
{
    public CreateTenantCommandValidator()
    {
        RuleFor(x => x.Identifier).NotEmpty().MaximumLength(64).Matches("^[a-z0-9-]+$");
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
    }
}
