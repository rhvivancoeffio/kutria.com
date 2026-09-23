using Commerce.Application.Abstracts;
using Commerce.Infrastructure.Agents;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;

namespace Commerce.Infrastructure.Agents.History;

/// <summary>
/// MAF ChatHistoryProvider backed by <see cref="IChatThreadStore"/>. Thread identity comes from <see cref="ChatTurnScope"/>.
/// </summary>
public sealed class RedisChatHistoryProvider(
    IChatThreadStore store,
    ILogger<RedisChatHistoryProvider> logger) : ChatHistoryProvider
{
    protected override async ValueTask<IEnumerable<ChatMessage>> ProvideChatHistoryAsync(
        InvokingContext context,
        CancellationToken cancellationToken = default)
    {
        var turn = ChatTurnScope.Current;
        if (turn is null)
        {
            return [];
        }

        var history = await store.GetAsync(turn.TenantId, turn.Audience, turn.ThreadId, cancellationToken)
            .ConfigureAwait(false);
        return history.Select(ToChatMessage);
    }

    protected override async ValueTask StoreChatHistoryAsync(
        InvokedContext context,
        CancellationToken cancellationToken = default)
    {
        var turn = ChatTurnScope.Current;
        if (turn is null || context.InvokeException is not null)
        {
            return;
        }

        var existing = await store.GetAsync(turn.TenantId, turn.Audience, turn.ThreadId, cancellationToken)
            .ConfigureAwait(false);
        var merged = existing.ToList();
        foreach (var message in context.RequestMessages.Concat(context.ResponseMessages ?? []))
        {
            var mapped = ToThreadMessage(message);
            if (mapped is null)
            {
                continue;
            }

            if (merged.Count > 0
                && string.Equals(merged[^1].Role, mapped.Role, StringComparison.OrdinalIgnoreCase)
                && string.Equals(merged[^1].Text, mapped.Text, StringComparison.Ordinal))
            {
                continue;
            }

            merged.Add(mapped);
        }

        await store.SaveAsync(turn.TenantId, turn.Audience, turn.ThreadId, merged, cancellationToken)
            .ConfigureAwait(false);
        logger.LogDebug(
            "Chat history stored for {ThreadId} agent {Agent} messages {Count}",
            turn.ThreadId,
            context.Agent.Name ?? context.Agent.Id,
            merged.Count);
    }

    internal static ChatMessage ToChatMessage(ChatThreadMessage message)
    {
        var role = message.Role.ToLowerInvariant() switch
        {
            "assistant" => ChatRole.Assistant,
            "system" => ChatRole.System,
            "tool" => ChatRole.Tool,
            _ => ChatRole.User
        };
        return new ChatMessage(role, message.Text);
    }

    internal static ChatThreadMessage? ToThreadMessage(ChatMessage message)
    {
        var text = message.Text;
        if (string.IsNullOrWhiteSpace(text))
        {
            return null;
        }

        var role = message.Role == ChatRole.Assistant ? "assistant"
            : message.Role == ChatRole.System ? "system"
            : message.Role == ChatRole.Tool ? "tool"
            : "user";
        return new ChatThreadMessage(role, text);
    }
}
