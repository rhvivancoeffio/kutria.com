namespace Commerce.Application.Features.Agents.ListAgents;

public sealed record AgentListItem(
    string Key,
    string Kind,
    string Name,
    string? Model,
    string? Queue,
    bool IsTenantOverride,
    IReadOnlyList<string> Tools);

public sealed record ListAgentsResult(IReadOnlyList<AgentListItem> Agents);
