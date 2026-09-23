using System.Text.Json;
using MediatR;
using Commerce.Application.Abstracts;
using Commerce.Application.Features.DataIngestion;
using Commerce.Application.Features.DataIngestion.ProcessCatalogVectorIngest;
using Commerce.Infrastructure.Accounts;
using Commerce.Infrastructure.Persistence;

namespace Commerce.Worker.Workers;

public sealed class CatalogVectorIngestWorker(
    IServiceScopeFactory scopes,
    ILogger<CatalogVectorIngestWorker> logger) : BackgroundService
{
    private static readonly JsonSerializerOptions Json = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private static readonly TimeSpan VisibilityTimeout = TimeSpan.FromMinutes(5);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("Catalog vector ingest listening on {Queue}", DataIngestQueues.CatalogVector);
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = scopes.CreateScope();
                var queue = scope.ServiceProvider.GetRequiredService<IMessageQueue>();
                var message = await queue.ReceiveAsync(
                    DataIngestQueues.CatalogVector,
                    stoppingToken,
                    VisibilityTimeout);
                if (message is null)
                {
                    await Task.Delay(TimeSpan.FromSeconds(1), stoppingToken);
                    continue;
                }

                try
                {
                    await HandleAsync(scope.ServiceProvider, message, stoppingToken);
                    await queue.AcknowledgeAsync(
                        DataIngestQueues.CatalogVector,
                        message.MessageId,
                        message.PopReceipt,
                        stoppingToken);
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    logger.LogWarning(ex, "Catalog vector ingest failed a message. Releasing for retry.");
                    await Task.Delay(TimeSpan.FromSeconds(2), stoppingToken);
                    await queue.ReleaseForRetryAsync(
                        DataIngestQueues.CatalogVector,
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
                logger.LogWarning(ex, "Catalog vector ingest failed a poll. Retrying.");
                await Task.Delay(TimeSpan.FromSeconds(2), stoppingToken);
            }
        }
    }

    private async Task HandleAsync(IServiceProvider services, QueuedMessage message, CancellationToken cancellationToken)
    {
        CatalogVectorIngestMessage? body;
        try
        {
            body = JsonSerializer.Deserialize<CatalogVectorIngestMessage>(message.Body.Span, Json);
        }
        catch (JsonException ex)
        {
            logger.LogWarning(ex, "Catalog vector ingest ignored a malformed message.");
            return;
        }

        if (body is null
            || body.IntegrationId == Guid.Empty
            || string.IsNullOrWhiteSpace(body.Tenant)
            || string.IsNullOrWhiteSpace(body.ProductId)
            || string.IsNullOrWhiteSpace(body.ExpectedContentHash))
        {
            logger.LogWarning("Catalog vector ingest ignored a message without required fields.");
            return;
        }

        var store = services.GetRequiredService<ITenantStore>();
        var tenant = await store.GetByIdentifierAsync(body.Tenant, cancellationToken)
            ?? await store.GetByIdAsync(body.Tenant, cancellationToken);
        if (tenant is null)
        {
            logger.LogWarning("Catalog vector ingest ignored unknown tenant {Tenant}.", body.Tenant);
            return;
        }

        TenantBootstrap.SetCurrentTenant(services, tenant);
        using var _ = await WorkspaceBootstrap.UseFromIntegrationAsync(services, body.IntegrationId, cancellationToken);
        logger.LogInformation(
            "Catalog vector ingest processing. Tenant={Tenant} IntegrationId={IntegrationId} ProductId={ProductId} DequeueCount={DequeueCount}",
            body.Tenant,
            body.IntegrationId,
            body.ProductId,
            message.DequeueCount);

        var mediator = services.GetRequiredService<IMediator>();
        var result = await mediator.Send(
            new ProcessCatalogVectorIngestCommand(
                body.IntegrationId,
                body.ProductId,
                body.ExpectedContentHash,
                message.DequeueCount),
            cancellationToken);

        logger.LogInformation(
            "Catalog vector ingest done. ProductId={ProductId} Outcome={Outcome} SkuCount={SkuCount}",
            result.ProductId,
            result.Outcome,
            result.SkuCount);
    }
}
