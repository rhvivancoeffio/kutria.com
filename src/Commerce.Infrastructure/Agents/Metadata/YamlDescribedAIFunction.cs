using Microsoft.Extensions.AI;

namespace Commerce.Infrastructure.Agents.Metadata;

/// <summary>
/// Applies catalog (YAML) description over the factory-created function for the LLM.
/// </summary>
internal sealed class YamlDescribedAIFunction(AIFunction inner, string description) : DelegatingAIFunction(inner)
{
    public override string Description { get; } = description;
}
