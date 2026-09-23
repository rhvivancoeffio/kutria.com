using System.Text;
using System.Text.Json;
using Commerce.Application.Abstracts;
using Microsoft.Extensions.Logging;

namespace Commerce.Infrastructure.Agents.Events;

public sealed class QueueChatEventPublisher(
    IMessageQueue queue,
    ILogger<QueueChatEventPublisher> logger) : IChatEventPublisher
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    public async Task PublishAsync(ChatTurnEvent evt, CancellationToken cancellationToken = default)
    {
        try
        {
            var body = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(evt, Json));
            await queue.SendAsync(ChatQueues.Events, body, cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            logger.LogWarning(
                ex,
                "Chat event publish failed for thread {ThreadId} type {EventType}",
                evt.ThreadId,
                evt.EventType);
        }
    }
}
