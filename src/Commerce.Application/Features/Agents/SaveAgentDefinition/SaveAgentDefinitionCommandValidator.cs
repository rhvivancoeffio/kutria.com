using FluentValidation;

namespace Commerce.Application.Features.Agents.SaveAgentDefinition;

public sealed class SaveAgentDefinitionCommandValidator : AbstractValidator<SaveAgentDefinitionCommand>
{
    public SaveAgentDefinitionCommandValidator()
    {
        RuleFor(x => x.AgentKey).NotEmpty().MaximumLength(80);
        RuleFor(x => x.Yaml).NotEmpty().MaximumLength(20_000);
    }
}
