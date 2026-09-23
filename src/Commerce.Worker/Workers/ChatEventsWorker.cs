using System.Text.Json;
using Commerce.Application.Abstracts;

namespace Commerce.Worker.Workers;

public sealed class ChatEventsWorker(
    IServiceScopeFactory scopes,
    ILogger<ChatEventsWorker> logger) : BackgroundService
{
    private static readonly JsonSerializerOptions Json = new()
    {
        PropertyNameCaseInsensitive = true
    };

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("Chat events listening on {Queue}", ChatQueues.Events);
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = scopes.CreateScope();
                var queue = scope.ServiceProvider.GetRequiredService<IMessageQueue>();
                var store = scope.ServiceProvider.GetRequiredService<IChatEventTableStore>();
                var message = await queue.ReceiveAsync(ChatQueues.Events, stoppingToken);
                if (message is null)
                {
                    await Task.Delay(TimeSpan.FromSeconds(1), stoppingToken);
                    continue;
                }

                try
                {
                    await HandleAsync(store, message, stoppingToken);
                    await queue.AcknowledgeAsync(ChatQueues.Events, message.MessageId, message.PopReceipt, stoppingToken);
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    logger.LogWarning(ex, "Chat events failed a message. Releasing for retry.");
                    await Task.Delay(TimeSpan.FromSeconds(2), stoppingToken);
                    await queue.ReleaseForRetryAsync(ChatQueues.Events, message.MessageId, message.PopReceipt, stoppingToken);
                }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Chat events failed a poll. Retrying.");
                await Task.Delay(TimeSpan.FromSeconds(2), stoppingToken);
            }
        }
    }

    private async Task HandleAsync(IChatEventTableStore store, QueuedMessage message, CancellationToken cancellationToken)
    {
        ChatTurnEvent? body;
        try
        {
            body = JsonSerializer.Deserialize<ChatTurnEvent>(message.Body.Span, Json);
        }
        catch (JsonException ex)
        {
            logger.LogWarning(ex, "Chat events ignored a malformed message.");
            return;
        }

        if (body is null || string.IsNullOrWhiteSpace(body.ThreadId))
        {
            logger.LogWarning("Chat events ignored a message without thread.");
            return;
        }

        var entity = ChatEventKeys.FromTurnEvent(body);
        await store.UpsertAsync(entity, cancellationToken);

        logger.LogInformation(
            "Chat event {EventType} tenant {TenantId} thread {ThreadId} audience {Audience} agent {AgentKey} tools [{Tools}] latency_ms {LatencyMs} empty {Empty} error {Error}",
            body.EventType,
            body.TenantId,
            body.ThreadId,
            body.Audience,
            body.AgentKey ?? "(none)",
            body.Tools.Count == 0 ? "(none)" : string.Join(", ", body.Tools),
            body.LatencyMs,
            body.Empty,
            body.Error ?? "(none)");
    }
}
