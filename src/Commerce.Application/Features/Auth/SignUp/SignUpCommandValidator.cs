using FluentValidation;

namespace Commerce.Application.Features.Auth.SignUp;

public sealed class SignUpCommandValidator : AbstractValidator<SignUpCommand>
{
    public SignUpCommandValidator()
    {
        RuleFor(x => x.Identifier)
            .NotEmpty()
            .MaximumLength(80)
            .Must(id => TenantIdentifierRules.IsFormatValid(TenantIdentifierRules.Slugify(id)))
            .WithMessage("Ese nombre no genera un identificador válido. Usa al menos 3 letras o números.")
            .Must(id => !TenantIdentifierRules.IsReserved(TenantIdentifierRules.Slugify(id)))
            .WithMessage("Ese identificador está reservado.");
        RuleFor(x => x.Email).NotEmpty().EmailAddress().MaximumLength(256);
        RuleFor(x => x.Password).NotEmpty().MinimumLength(1).MaximumLength(128);
        RuleFor(x => x.DisplayName).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Name).MaximumLength(200);
        RuleFor(x => x.PlanKey).MaximumLength(64);
    }
}
