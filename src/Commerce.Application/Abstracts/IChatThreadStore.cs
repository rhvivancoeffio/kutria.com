namespace Commerce.Application.Abstracts;

public static class ChatQueues
{
    public const string Events = "chat-events";
}

public static class ChatThreadKeys
{
    public static string Build(string tenantId, string audience, string threadId)
        => $"commerce:chat:{tenantId}:{audience}:{threadId}";

    public static string Index(string tenantId, string audience)
        => $"commerce:chat:index:{tenantId}:{audience}";
}

public sealed record ChatThreadMessage(string Role, string Text);

public sealed record ChatThreadSummary(
    string ThreadId,
    string? Title,
    DateTimeOffset UpdatedAt,
    int MessageCount);

public interface IChatThreadStore
{
    Task<IReadOnlyList<ChatThreadMessage>> GetAsync(
        string tenantId,
        string audience,
        string threadId,
        CancellationToken cancellationToken = default);

    Task SaveAsync(
        string tenantId,
        string audience,
        string threadId,
        IReadOnlyList<ChatThreadMessage> messages,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ChatThreadSummary>> ListAsync(
        string tenantId,
        string audience,
        int take = 50,
        CancellationToken cancellationToken = default);
}
