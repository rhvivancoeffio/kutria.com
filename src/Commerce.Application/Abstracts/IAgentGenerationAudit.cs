namespace Commerce.Application.Abstracts;

public static class AgentAuditQueues
{
    public const string Generations = "agent-generation-audit";
}

public sealed record AgentGenerationAuditMessage(
    string TenantId,
    string ProcessId,
    string AgentKey,
    string Audience,
    string Kind,
    string Status,
    DateTimeOffset OccurredAtUtc,
    string? InputSummaryJson = null,
    string? Error = null,
    string? OutputHash = null);

public interface IAgentGenerationAuditPublisher
{
    Task PublishAsync(AgentGenerationAuditMessage message, CancellationToken cancellationToken = default);
}

public interface IAgentGenerationAuditStore
{
    Task UpsertAsync(AgentGenerationAuditMessage message, CancellationToken cancellationToken = default);
}
