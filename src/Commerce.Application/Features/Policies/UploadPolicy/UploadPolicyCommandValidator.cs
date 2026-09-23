using FluentValidation;

namespace Commerce.Application.Features.Policies.UploadPolicy;

public sealed class UploadPolicyCommandValidator : AbstractValidator<UploadPolicyCommand>
{
    private static readonly string[] Types = ["return", "faq", "terms", "privacy", "shipping"];
    private static readonly string[] Extensions = [".pdf", ".docx", ".txt", ".json"];

    public UploadPolicyCommandValidator()
    {
        RuleFor(x => x.Type).Must(type => Types.Contains(type, StringComparer.OrdinalIgnoreCase))
            .WithMessage("Type must be return, faq, terms, privacy or shipping.");
        RuleFor(x => x.EffectiveFrom).Must(value => DateOnly.TryParse(value, out _))
            .WithMessage("Effective from must be a date.");
        RuleFor(x => x.FileName).Must(name => Extensions.Contains(Path.GetExtension(name), StringComparer.OrdinalIgnoreCase))
            .WithMessage("Excel and legacy Word are not supported yet. Use PDF, Word, TXT or JSON.");
        RuleFor(x => x.Content).NotEmpty().Must(content => content.Length <= 20_000_000);
    }
}
