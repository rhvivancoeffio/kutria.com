using System.Text.Json;
using MediatR;
using Commerce.Application.Abstracts;
using Commerce.Application.Features.Brains.ProcessBrainIngest;
using Commerce.Infrastructure.Accounts;
using Commerce.Infrastructure.Persistence;

namespace Commerce.Worker.Workers;

public sealed class BrainIngestWorker(
    IServiceScopeFactory scopes,
    ILogger<BrainIngestWorker> logger) : BackgroundService
{
    private static readonly JsonSerializerOptions Json = new()
    {
        PropertyNameCaseInsensitive = true
    };

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("Brain ingest listening on {Queue}", BrainQueues.Ingest);
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = scopes.CreateScope();
                var queue = scope.ServiceProvider.GetRequiredService<IMessageQueue>();
                var message = await queue.ReceiveAsync(BrainQueues.Ingest, stoppingToken);
                if (message is null)
                {
                    await Task.Delay(TimeSpan.FromSeconds(1), stoppingToken);
                    continue;
                }

                try
                {
                    await HandleAsync(scope.ServiceProvider, message, stoppingToken);
                    await queue.AcknowledgeAsync(BrainQueues.Ingest, message.MessageId, message.PopReceipt, stoppingToken);
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    logger.LogWarning(ex, "Brain ingest failed a message. Releasing for retry.");
                    await Task.Delay(TimeSpan.FromSeconds(2), stoppingToken);
                    await queue.ReleaseForRetryAsync(BrainQueues.Ingest, message.MessageId, message.PopReceipt, stoppingToken);
                }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Brain ingest failed a poll. Retrying.");
                await Task.Delay(TimeSpan.FromSeconds(2), stoppingToken);
            }
        }
    }

    private async Task HandleAsync(IServiceProvider services, QueuedMessage message, CancellationToken cancellationToken)
    {
        BrainIngestMessage? body;
        try
        {
            body = JsonSerializer.Deserialize<BrainIngestMessage>(message.Body.Span, Json);
        }
        catch (JsonException ex)
        {
            logger.LogWarning(ex, "Brain ingest ignored a malformed message.");
            return;
        }

        if (body is null || body.JobId == Guid.Empty || string.IsNullOrWhiteSpace(body.Tenant))
        {
            logger.LogWarning("Brain ingest ignored a message without job or tenant.");
            return;
        }

        var store = services.GetRequiredService<ITenantStore>();
        var tenant = await store.GetByIdentifierAsync(body.Tenant, cancellationToken)
            ?? await store.GetByIdAsync(body.Tenant, cancellationToken);
        if (tenant is null)
        {
            logger.LogWarning("Brain ingest ignored job {JobId}. Tenant {Tenant} was not found.", body.JobId, body.Tenant);
            return;
        }

        TenantBootstrap.SetCurrentTenant(services, tenant);
        using var _ = await WorkspaceBootstrap.UseFromBrainJobAsync(services, body.JobId, cancellationToken);
        var mediator = services.GetRequiredService<IMediator>();
        await mediator.Send(new ProcessBrainIngestCommand(body.JobId, body.Step), cancellationToken);
    }
}
