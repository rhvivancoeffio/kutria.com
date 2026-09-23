using System.Text.Json;
using Commerce.Application.Abstracts;

namespace Commerce.Infrastructure.EventStreams;

/// <summary>Maps agent yield events into thrifty <see cref="IEventStreamStore"/> rows (no token chunks).</summary>
public static class AgentStreamEventAppender
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    public static bool ShouldPersist(string type)
        => type is "status" or "tool" or "output" or "done" or "error";

    public static async Task AppendIfNeededAsync(
        IEventStreamStore store,
        string tenantId,
        string streamId,
        AgentStreamEvent evt,
        string fallbackThreadId,
        CancellationToken cancellationToken)
    {
        if (!ShouldPersist(evt.Type) || string.IsNullOrWhiteSpace(streamId))
            return;

        string? data = null;
        if (!string.IsNullOrWhiteSpace(evt.ToolName)
            || !string.IsNullOrWhiteSpace(evt.AgentKey)
            || !string.IsNullOrWhiteSpace(evt.ThreadId))
        {
            data = JsonSerializer.Serialize(new
            {
                toolName = evt.ToolName,
                agentKey = evt.AgentKey,
                threadId = evt.ThreadId ?? fallbackThreadId
            }, Json);
        }

        await store.AppendAsync(
            tenantId,
            streamId,
            evt.Type,
            evt.Text,
            data,
            cancellationToken);
    }
}
