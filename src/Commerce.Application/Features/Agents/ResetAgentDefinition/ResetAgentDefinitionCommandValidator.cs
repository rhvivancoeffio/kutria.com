using FluentValidation;

namespace Commerce.Application.Features.Agents.ResetAgentDefinition;

public sealed class ResetAgentDefinitionCommandValidator : AbstractValidator<ResetAgentDefinitionCommand>
{
    public ResetAgentDefinitionCommandValidator()
    {
        RuleFor(x => x.AgentKey).NotEmpty().MaximumLength(80);
    }
}
