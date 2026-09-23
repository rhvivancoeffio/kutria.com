using Commerce.Application.Abstracts;
using Microsoft.Agents.AI;
using Microsoft.Extensions.Logging;
using Commerce.Infrastructure.Agents;

namespace Commerce.Infrastructure.Agents.Events;

/// <summary>
/// Publishes per-agent invocation telemetry after MAF completes an agent run.
/// </summary>
public sealed class ChatEventsAIContextProvider(
    IChatEventPublisher publisher,
    ILogger<ChatEventsAIContextProvider> logger) : AIContextProvider
{
    protected override async ValueTask StoreAIContextAsync(
        InvokedContext context,
        CancellationToken cancellationToken = default)
    {
        var turn = ChatTurnScope.Current;
        if (turn is null)
        {
            return;
        }

        var responseText = string.Join(
            '\n',
            (context.ResponseMessages ?? []).Select(m => m.Text).Where(t => !string.IsNullOrWhiteSpace(t)));
        var tools = ExtractToolNames(context.ResponseMessages ?? []).ToArray();
        var evt = new ChatTurnEvent(
            EventType: "agent_invoked",
            TenantId: turn.TenantId,
            ThreadId: turn.ThreadId,
            Audience: turn.Audience,
            AgentKey: context.Agent.Name ?? context.Agent.Id,
            Tools: tools,
            LatencyMs: 0,
            Empty: string.IsNullOrWhiteSpace(responseText),
            Error: context.InvokeException?.Message,
            OccurredAt: DateTimeOffset.UtcNow);

        try
        {
            await publisher.PublishAsync(evt, cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            logger.LogDebug(ex, "Chat agent_invoked publish skipped for {Agent}", context.Agent.Name);
        }
    }

    private static IEnumerable<string> ExtractToolNames(IEnumerable<Microsoft.Extensions.AI.ChatMessage> messages)
    {
        foreach (var message in messages)
        {
            if (message.Contents is null)
            {
                continue;
            }

            foreach (var content in message.Contents)
            {
                if (content is Microsoft.Extensions.AI.FunctionCallContent call && !string.IsNullOrWhiteSpace(call.Name))
                {
                    yield return call.Name;
                }
            }
        }
    }
}
