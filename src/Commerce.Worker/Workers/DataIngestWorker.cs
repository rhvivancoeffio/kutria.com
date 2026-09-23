using System.Text;
using System.Text.Json;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Commerce.Application.Abstracts;
using Commerce.Application.Features.DataIngestion;
using Commerce.Application.Features.DataIngestion.ProcessDataIngest;
using Commerce.Infrastructure.Accounts;
using Commerce.Infrastructure.Persistence;

namespace Commerce.Worker.Workers;

public sealed class DataIngestWorker(
    IServiceScopeFactory scopes,
    ILogger<DataIngestWorker> logger) : BackgroundService
{
    private static readonly JsonSerializerOptions Json = new()
    {
        PropertyNameCaseInsensitive = true
    };

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("Data ingest listening on {Queue}", DataIngestQueues.Ingest);
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = scopes.CreateScope();
                var queue = scope.ServiceProvider.GetRequiredService<IMessageQueue>();
                var message = await queue.ReceiveAsync(DataIngestQueues.Ingest, stoppingToken);
                if (message is null)
                {
                    await Task.Delay(TimeSpan.FromSeconds(1), stoppingToken);
                    continue;
                }

                try
                {
                    await HandleAsync(scope.ServiceProvider, message, stoppingToken);
                    await queue.AcknowledgeAsync(DataIngestQueues.Ingest, message.MessageId, message.PopReceipt, stoppingToken);
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    logger.LogWarning(ex, "Data ingest failed a message. Releasing for retry.");
                    await Task.Delay(TimeSpan.FromSeconds(2), stoppingToken);
                    await queue.ReleaseForRetryAsync(DataIngestQueues.Ingest, message.MessageId, message.PopReceipt, stoppingToken);
                }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Data ingest failed a poll. Retrying.");
                await Task.Delay(TimeSpan.FromSeconds(2), stoppingToken);
            }
        }
    }

    private async Task HandleAsync(IServiceProvider services, QueuedMessage message, CancellationToken cancellationToken)
    {
        DataIngestMessage? body;
        try
        {
            body = JsonSerializer.Deserialize<DataIngestMessage>(message.Body.Span, Json);
        }
        catch (JsonException ex)
        {
            logger.LogWarning(ex, "Data ingest ignored a malformed message.");
            return;
        }

        if (body is null || body.IntegrationId == Guid.Empty || string.IsNullOrWhiteSpace(body.Tenant) || string.IsNullOrWhiteSpace(body.Kind))
        {
            logger.LogWarning("Data ingest ignored a message without tenant, integration, or kind.");
            return;
        }

        var store = services.GetRequiredService<ITenantStore>();
        var tenant = await store.GetByIdentifierAsync(body.Tenant, cancellationToken)
            ?? await store.GetByIdAsync(body.Tenant, cancellationToken);
        if (tenant is null)
        {
            logger.LogWarning("Data ingest ignored unknown tenant {Tenant}.", body.Tenant);
            return;
        }

        TenantBootstrap.SetCurrentTenant(services, tenant);
        using var _ = await WorkspaceBootstrap.UseFromIntegrationAsync(services, body.IntegrationId, cancellationToken);
        logger.LogInformation(
            "Data ingest processing message. Tenant={Tenant} IntegrationId={IntegrationId} Kind={Kind}",
            body.Tenant,
            body.IntegrationId,
            body.Kind);
        var mediator = services.GetRequiredService<IMediator>();
        var result = await mediator.Send(new ProcessDataIngestCommand(body.IntegrationId, body.Kind), cancellationToken);
        logger.LogInformation(
            "Data ingest message done. IntegrationId={IntegrationId} Kind={Kind} Upserted={Upserted}",
            result.IntegrationId,
            result.Kind,
            result.Upserted);
    }
}

public sealed class DataIngestDispatcher(
    IServiceScopeFactory scopes,
    IConfiguration configuration,
    ILogger<DataIngestDispatcher> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var minutes = configuration.GetValue("DataIngestion:DispatchIntervalMinutes", 15);
        if (minutes < 1)
            minutes = 15;

        logger.LogInformation("Data ingest dispatcher every {Minutes} minutes", minutes);
        using var timer = new PeriodicTimer(TimeSpan.FromMinutes(minutes));

        // Initial delay so API/DB are ready
        try
        {
            await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken);
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            return;
        }

        await DispatchOnceAsync(stoppingToken);

        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            await DispatchOnceAsync(stoppingToken);
        }
    }

    private async Task DispatchOnceAsync(CancellationToken cancellationToken)
    {
        try
        {
            using var scope = scopes.CreateScope();
            var tenants = scope.ServiceProvider.GetRequiredService<ITenantStore>();
            var all = await tenants.ListAsync(cancellationToken);
            var enqueued = 0;

            foreach (var tenant in all)
            {
                using var tenantScope = scopes.CreateScope();
                TenantBootstrap.SetCurrentTenant(tenantScope.ServiceProvider, tenant);
                var db = tenantScope.ServiceProvider.GetRequiredService<ICommerceDbContext>();
                var queue = tenantScope.ServiceProvider.GetRequiredService<IMessageQueue>();

                var integrations = await db.Integrations.AsNoTracking()
                    .Where(x => x.IsActive)
                    .ToListAsync(cancellationToken);

                var tenantKey = tenant.Identifier ?? tenant.Id;
                if (string.IsNullOrWhiteSpace(tenantKey))
                    continue;

                foreach (var integration in integrations)
                {
                    if (!DataIngestCredentials.TryParse(integration.SettingsJson, out _))
                        continue;

                    foreach (var kind in new[] { DataIngestKinds.Catalog, DataIngestKinds.Orders })
                    {
                        var message = new DataIngestMessage(tenantKey, integration.Id, kind);
                        var body = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(message));
                        await queue.SendAsync(DataIngestQueues.Ingest, body, cancellationToken);
                        enqueued++;
                    }
                }
            }

            logger.LogInformation(
                "Data ingest dispatch completed for {TenantCount} tenants, enqueued {Count} messages",
                all.Count,
                enqueued);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogWarning(ex, "Data ingest dispatch failed");
        }
    }
}
