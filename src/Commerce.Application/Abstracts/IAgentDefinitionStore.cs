namespace Commerce.Application.Abstracts;

public interface IAgentDefinitionStore
{
    Task<IReadOnlyList<AgentDefinition>> ListAsync(string tenantId, CancellationToken cancellationToken = default);

    Task<AgentDefinition?> GetAsync(string tenantId, string agentKey, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<AgentDefinition>> ListSystemAsync(CancellationToken cancellationToken = default);

    Task<AgentDefinition> SaveTenantYamlAsync(
        string tenantId,
        string agentKey,
        string yaml,
        CancellationToken cancellationToken = default);

    Task ResetTenantAsync(string tenantId, string agentKey, CancellationToken cancellationToken = default);
}
