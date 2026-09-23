using System.Text.Json;
using MediatR;
using Commerce.Application.Abstracts;
using Commerce.Application.Features.DataIngestion;
using Commerce.Application.Features.DataIngestion.ProcessCatalogImageVector;
using Commerce.Infrastructure.Accounts;
using Commerce.Infrastructure.Persistence;
using Microsoft.Extensions.Configuration;

namespace Commerce.Worker.Workers;

public sealed class CatalogImageVectorIngestWorker(
    IServiceScopeFactory scopes,
    IConfiguration configuration,
    ILogger<CatalogImageVectorIngestWorker> logger) : BackgroundService
{
    private static readonly JsonSerializerOptions Json = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private static readonly TimeSpan VisibilityTimeout = TimeSpan.FromMinutes(10);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var maxDegree = Math.Clamp(configuration.GetValue("CatalogImage:MaxDegree", 2), 1, 8);
        logger.LogInformation(
            "Catalog image vector ingest listening on {Queue} MaxDegree={MaxDegree}",
            DataIngestQueues.CatalogImage,
            maxDegree);

        using var gate = new SemaphoreSlim(maxDegree, maxDegree);
        var inflight = new List<Task>();

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await gate.WaitAsync(stoppingToken);
                var scope = scopes.CreateScope();
                var queue = scope.ServiceProvider.GetRequiredService<IMessageQueue>();
                var message = await queue.ReceiveAsync(
                    DataIngestQueues.CatalogImage,
                    stoppingToken,
                    VisibilityTimeout);
                if (message is null)
                {
                    scope.Dispose();
                    gate.Release();
                    await Task.Delay(TimeSpan.FromSeconds(1), stoppingToken);
                    continue;
                }

                var task = ProcessOneAsync(scope, queue, message, gate, stoppingToken);
                lock (inflight)
                {
                    inflight.Add(task);
                    inflight.RemoveAll(t => t.IsCompleted);
                }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Catalog image vector ingest failed a poll. Retrying.");
                await Task.Delay(TimeSpan.FromSeconds(2), stoppingToken);
            }
        }

        Task[] pending;
        lock (inflight)
            pending = inflight.ToArray();
        try
        {
            await Task.WhenAll(pending);
        }
        catch
        {
            // Individual tasks log their own failures.
        }
    }

    private async Task ProcessOneAsync(
        IServiceScope scope,
        IMessageQueue queue,
        QueuedMessage message,
        SemaphoreSlim gate,
        CancellationToken stoppingToken)
    {
        try
        {
            await HandleAsync(scope.ServiceProvider, message, stoppingToken);
            await queue.AcknowledgeAsync(
                DataIngestQueues.CatalogImage,
                message.MessageId,
                message.PopReceipt,
                stoppingToken);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogWarning(ex, "Catalog image vector ingest failed a message. Releasing for retry.");
            await Task.Delay(TimeSpan.FromSeconds(2), stoppingToken);
            await queue.ReleaseForRetryAsync(
                DataIngestQueues.CatalogImage,
                message.MessageId,
                message.PopReceipt,
                stoppingToken);
        }
        finally
        {
            scope.Dispose();
            gate.Release();
        }
    }

    private async Task HandleAsync(IServiceProvider services, QueuedMessage message, CancellationToken cancellationToken)
    {
        CatalogImageVectorMessage? body;
        try
        {
            body = JsonSerializer.Deserialize<CatalogImageVectorMessage>(message.Body.Span, Json);
        }
        catch (JsonException ex)
        {
            logger.LogWarning(ex, "Catalog image vector ingest ignored a malformed message.");
            return;
        }

        if (body is null
            || body.IntegrationId == Guid.Empty
            || string.IsNullOrWhiteSpace(body.Tenant)
            || string.IsNullOrWhiteSpace(body.ProductId)
            || string.IsNullOrWhiteSpace(body.ImageUrl)
            || string.IsNullOrWhiteSpace(body.ExpectedContentHash)
            || body.Skus is null
            || body.Skus.Count == 0)
        {
            logger.LogWarning("Catalog image vector ingest ignored a message without required fields.");
            return;
        }

        var store = services.GetRequiredService<ITenantStore>();
        var tenant = await store.GetByIdentifierAsync(body.Tenant, cancellationToken)
            ?? await store.GetByIdAsync(body.Tenant, cancellationToken);
        if (tenant is null)
        {
            logger.LogWarning("Catalog image vector ingest ignored unknown tenant {Tenant}.", body.Tenant);
            return;
        }

        TenantBootstrap.SetCurrentTenant(services, tenant);
        using var _ = await WorkspaceBootstrap.UseFromIntegrationAsync(services, body.IntegrationId, cancellationToken);
        logger.LogInformation(
            "Catalog image vector ingest processing. Tenant={Tenant} IntegrationId={IntegrationId} ProductId={ProductId} ImageUrl={ImageUrl} SkuCount={SkuCount} DequeueCount={DequeueCount}",
            body.Tenant,
            body.IntegrationId,
            body.ProductId,
            Truncate(body.ImageUrl),
            body.Skus.Count,
            message.DequeueCount);

        var mediator = services.GetRequiredService<IMediator>();
        var result = await mediator.Send(
            new ProcessCatalogImageVectorCommand(
                body.IntegrationId,
                body.ProductId,
                body.ImageUrl,
                body.Skus,
                body.ExpectedContentHash,
                message.DequeueCount),
            cancellationToken);

        logger.LogInformation(
            "Catalog image vector ingest done. ProductId={ProductId} Outcome={Outcome} PatchedSkuCount={PatchedSkuCount}",
            result.ProductId,
            result.Outcome,
            result.PatchedSkuCount);
    }

    private static string Truncate(string? value, int max = 160)
    {
        if (string.IsNullOrEmpty(value))
            return string.Empty;
        return value.Length <= max ? value : value[..max] + "…";
    }
}
