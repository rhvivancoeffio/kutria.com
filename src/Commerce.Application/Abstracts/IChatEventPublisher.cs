namespace Commerce.Application.Abstracts;

public sealed record ChatTurnEvent(
    string EventType,
    string TenantId,
    string ThreadId,
    string Audience,
    string? AgentKey,
    IReadOnlyList<string> Tools,
    long LatencyMs,
    bool Empty,
    string? Error,
    DateTimeOffset OccurredAt);

public interface IChatEventPublisher
{
    Task PublishAsync(ChatTurnEvent evt, CancellationToken cancellationToken = default);
}
