using Commerce.Application.Abstracts;

namespace Commerce.Worker.Workers;

/// <summary>Deletes completed event-stream partitions after EventStreams:Ttl (low-cost hygiene).</summary>
public sealed class EventStreamGcWorker(
    IServiceScopeFactory scopes,
    ILogger<EventStreamGcWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("Event stream GC worker started.");
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await using var scope = scopes.CreateAsyncScope();
                var store = scope.ServiceProvider.GetRequiredService<IEventStreamStore>();
                var due = await store.ListDueForDeleteAsync(DateTimeOffset.UtcNow, 40, stoppingToken);
                foreach (var (tenantId, streamId) in due)
                {
                    try
                    {
                        await store.DeleteStreamAsync(tenantId, streamId, stoppingToken);
                        logger.LogInformation(
                            "Event stream GC deleted tenant={TenantId} stream={StreamId}",
                            tenantId,
                            streamId);
                    }
                    catch (Exception ex) when (ex is not OperationCanceledException)
                    {
                        logger.LogWarning(ex, "Event stream GC failed for {StreamId}", streamId);
                    }
                }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Event stream GC loop failed. Retrying.");
            }

            try
            {
                await Task.Delay(TimeSpan.FromMinutes(15), stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
        }
    }
}
