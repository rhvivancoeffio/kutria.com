using Microsoft.Extensions.AI;
using Commerce.Application.Abstracts;

namespace Commerce.Infrastructure.Agents;

/// <summary>
/// One conversational or workflow agent. Implementations live under
/// <c>Agents/{AgentName}/</c> and are registered as <see cref="IAgentModule"/>.
/// </summary>
public interface IAgentModule
{
    string Key { get; }

    IReadOnlyList<AITool> CreateTools(AgentDefinition definition, string tenantId, IServiceProvider services);

    Task ExecuteWorkflowAsync(
        AgentDefinition definition,
        string payload,
        IServiceProvider services,
        CancellationToken cancellationToken);
}
