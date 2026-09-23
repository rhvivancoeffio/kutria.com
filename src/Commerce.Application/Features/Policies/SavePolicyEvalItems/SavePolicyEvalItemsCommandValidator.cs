using FluentValidation;

namespace Commerce.Application.Features.Policies.SavePolicyEvalItems;

public sealed class SavePolicyEvalItemsCommandValidator : AbstractValidator<SavePolicyEvalItemsCommand>
{
    private static readonly string[] Types = ["return", "faq", "terms", "privacy", "shipping"];

    public SavePolicyEvalItemsCommandValidator()
    {
        RuleForEach(x => x.Items).ChildRules(item =>
        {
            item.RuleFor(x => x.Question).NotEmpty().MaximumLength(500);
            item.RuleFor(x => x.Type)
                .Must(type => string.IsNullOrWhiteSpace(type) || Types.Contains(type, StringComparer.OrdinalIgnoreCase))
                .WithMessage("Policy eval type is not supported.");
        });
    }
}
