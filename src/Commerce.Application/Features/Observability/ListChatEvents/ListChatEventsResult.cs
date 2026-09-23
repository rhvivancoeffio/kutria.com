namespace Commerce.Application.Features.Observability.ListChatEvents;

public sealed record ListChatEventsItem(
    string Id,
    string EventType,
    string ThreadId,
    string Audience,
    string? AgentKey,
    IReadOnlyList<string> Tools,
    long LatencyMs,
    bool Empty,
    string? Error,
    DateTimeOffset OccurredAt);

public sealed record ListChatEventsResult(
    IReadOnlyList<ListChatEventsItem> Items,
    int PageSize,
    string? NextToken,
    bool HasMore);
