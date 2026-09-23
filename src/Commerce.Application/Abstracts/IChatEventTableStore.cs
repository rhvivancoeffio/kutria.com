namespace Commerce.Application.Abstracts;

public sealed record ChatEventEntity(
    string PartitionKey,
    string RowKey,
    string EventType,
    string ThreadId,
    string Audience,
    string? AgentKey,
    string Tools,
    long LatencyMs,
    bool Empty,
    string? Error,
    DateTimeOffset OccurredAt);

public sealed record ChatEventListResult(
    IReadOnlyList<ChatEventEntity> Items,
    int PageSize,
    string? NextToken,
    bool HasMore);

public interface IChatEventTableStore
{
    Task UpsertAsync(ChatEventEntity entity, CancellationToken cancellationToken = default);

    Task<ChatEventListResult> ListAsync(
        string tenantId,
        int pageSize,
        string? nextToken,
        string? threadId = null,
        string? eventType = null,
        bool? emptyOnly = null,
        CancellationToken cancellationToken = default);
}

public static class ChatEventKeys
{
    public static string Partition(string tenantId) => tenantId.Trim();

    public static string RowKey(DateTimeOffset occurredAt, Guid id)
    {
        var inverted = (DateTimeOffset.MaxValue.UtcTicks - occurredAt.UtcTicks).ToString("D19");
        return $"{inverted}_{id:N}";
    }

    public static ChatEventEntity FromTurnEvent(ChatTurnEvent evt, Guid? id = null)
    {
        var occurredAt = evt.OccurredAt == default ? DateTimeOffset.UtcNow : evt.OccurredAt;
        var rowId = id ?? Guid.NewGuid();
        return new ChatEventEntity(
            Partition(evt.TenantId),
            RowKey(occurredAt, rowId),
            evt.EventType,
            evt.ThreadId,
            evt.Audience,
            evt.AgentKey,
            evt.Tools.Count == 0 ? string.Empty : string.Join(',', evt.Tools),
            evt.LatencyMs,
            evt.Empty,
            evt.Error,
            occurredAt);
    }
}
