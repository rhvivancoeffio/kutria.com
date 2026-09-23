using System.Text.Json;
using Commerce.Application.Abstracts;

namespace Commerce.Worker.Workers;

public sealed class AgentGenerationAuditWorker(
    IServiceScopeFactory scopes,
    ILogger<AgentGenerationAuditWorker> logger) : BackgroundService
{
    private static readonly JsonSerializerOptions Json = new()
    {
        PropertyNameCaseInsensitive = true
    };

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("Agent generation audit listening on {Queue}", AgentAuditQueues.Generations);
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = scopes.CreateScope();
                var queue = scope.ServiceProvider.GetRequiredService<IMessageQueue>();
                var store = scope.ServiceProvider.GetRequiredService<IAgentGenerationAuditStore>();
                var message = await queue.ReceiveAsync(AgentAuditQueues.Generations, stoppingToken);
                if (message is null)
                {
                    await Task.Delay(TimeSpan.FromSeconds(1), stoppingToken);
                    continue;
                }

                try
                {
                    await HandleAsync(store, message, stoppingToken);
                    await queue.AcknowledgeAsync(
                        AgentAuditQueues.Generations,
                        message.MessageId,
                        message.PopReceipt,
                        stoppingToken);
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    logger.LogWarning(ex, "Agent generation audit failed a message. Releasing for retry.");
                    await Task.Delay(TimeSpan.FromSeconds(2), stoppingToken);
                    await queue.ReleaseForRetryAsync(
                        AgentAuditQueues.Generations,
                        message.MessageId,
                        message.PopReceipt,
                        stoppingToken);
                }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Agent generation audit failed a poll. Retrying.");
                await Task.Delay(TimeSpan.FromSeconds(2), stoppingToken);
            }
        }
    }

    private async Task HandleAsync(
        IAgentGenerationAuditStore store,
        QueuedMessage message,
        CancellationToken cancellationToken)
    {
        AgentGenerationAuditMessage? body;
        try
        {
            body = JsonSerializer.Deserialize<AgentGenerationAuditMessage>(message.Body.Span, Json);
        }
        catch (JsonException ex)
        {
            logger.LogWarning(ex, "Agent generation audit ignored a malformed message.");
            return;
        }

        if (body is null
            || string.IsNullOrWhiteSpace(body.TenantId)
            || string.IsNullOrWhiteSpace(body.ProcessId))
        {
            logger.LogWarning("Agent generation audit ignored a message without tenant/process.");
            return;
        }

        await store.UpsertAsync(body, cancellationToken);
    }
}
