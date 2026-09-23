using System.Text;
using System.Text.Json;
using Finbuckle.MultiTenant.Abstractions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Commerce.Application.Abstracts;
using Commerce.Application.Common.Abstracts;
using Commerce.Application.Features.DataIngestion;
using Commerce.Domain.Tenants;

namespace Commerce.Application.Features.DataIngestion.ProcessDataIngest;

public sealed class ProcessDataIngestHandler(
    ICommerceDbContext db,
    IGravityStoreDataClient gravity,
    IDataIngestTableStore tables,
    IMessageQueue queue,
    IOptions<DataIngestionOptions> options,
    IMultiTenantContextAccessor<CommerceTenantInfo> tenantAccessor,
    ILogger<ProcessDataIngestHandler> logger)
    : ICommandHandler<ProcessDataIngestCommand, ProcessDataIngestResult>
{
    /// <summary>Gravity list page size; each page is followed by GetById for those rows, then batch upsert.</summary>
    private const int PageSize = 20;

    /// <summary>Bounded parallelism for Gravity GetById / meta (I/O-bound; avoids unbounded fan-out / 429s).</summary>
    private const int GetByIdMaxDegreeOfParallelism = 8;

    private static readonly JsonSerializerOptions Json = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

    public async Task<ProcessDataIngestResult> Handle(ProcessDataIngestCommand request, CancellationToken cancellationToken)
    {
        var tenant = tenantAccessor.MultiTenantContext?.TenantInfo
            ?? throw new InvalidOperationException("Tenant is required.");

        var tenantId = tenant.Id ?? tenant.Identifier
            ?? throw new InvalidOperationException("Tenant id is required.");
        var tenantKey = tenant.Identifier ?? tenant.Id
            ?? throw new InvalidOperationException("Tenant identifier is required.");

        logger.LogInformation(
            "DataIngest process start. TenantId={TenantId} TenantIdentifier={TenantIdentifier} IntegrationId={IntegrationId} Kind={Kind} TableStore={TableStore}",
            tenant.Id,
            tenant.Identifier,
            request.IntegrationId,
            request.Kind,
            tables.GetType().Name);

        var integration = await db.Integrations.AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == request.IntegrationId, cancellationToken)
            ?? throw new KeyNotFoundException($"Integration {request.IntegrationId} was not found.");

        logger.LogInformation(
            "DataIngest process integration. Provider={Provider} Name={Name} IsActive={IsActive}",
            integration.Provider,
            integration.Name,
            integration.IsActive);

        if (!DataIngestCredentials.TryParse(integration.SettingsJson, out var credentials, out var reason))
        {
            logger.LogError(
                "DataIngest process credentials parse failed. IntegrationId={IntegrationId} Reason={Reason}",
                integration.Id,
                reason);
            throw new InvalidOperationException($"Integration does not have Gravity API credentials (url + apiKey + organizationId). {reason}");
        }

        logger.LogInformation(
            "DataIngest process credentials OK. BaseUrl={BaseUrl} ApiKey={ApiKeyMask} ProductLimit={ProductLimit} OrderLimit={OrderLimit} GravityPageSize={PageSize}",
            credentials.BaseUrl,
            DataIngestCredentials.MaskApiKey(credentials.ApiKey),
            options.Value.EffectiveProductLimit?.ToString() ?? "unlimited",
            options.Value.EffectiveOrderLimit?.ToString() ?? "unlimited",
            PageSize);

        var partitionKey = DataIngestPartition.Key(tenantId, integration.Id);
        var kind = request.Kind.Trim().ToLowerInvariant();

        try
        {
            var upserted = kind switch
            {
                DataIngestKinds.Catalog => await SyncCatalogAsync(
                    credentials,
                    partitionKey,
                    tenantKey,
                    integration.Id,
                    cancellationToken),
                DataIngestKinds.Orders => await SyncOrdersAsync(credentials, partitionKey, cancellationToken),
                _ => throw new InvalidOperationException($"Unknown data ingest kind '{request.Kind}'.")
            };

            logger.LogInformation(
                "DataIngest process completed. IntegrationId={IntegrationId} Kind={Kind} PartitionKey={PartitionKey} Upserted={Upserted}",
                integration.Id,
                kind,
                partitionKey,
                upserted);

            return new ProcessDataIngestResult(integration.Id, kind, upserted);
        }
        catch (Exception ex)
        {
            logger.LogError(
                ex,
                "DataIngest process failed. IntegrationId={IntegrationId} Kind={Kind} PartitionKey={PartitionKey}",
                integration.Id,
                kind,
                partitionKey);
            throw;
        }
    }

    private async Task<int> SyncCatalogAsync(
        GravityStoreCredentials credentials,
        string partitionKey,
        string tenantKey,
        Guid integrationId,
        CancellationToken cancellationToken)
    {
        var max = options.Value.EffectiveProductLimit;
        var upserted = 0;
        var enqueued = 0;
        var page = 1;
        int pageCount;
        do
        {
            var remaining = max.HasValue ? max.Value - upserted : PageSize;
            if (remaining <= 0)
                break;

            var take = Math.Min(PageSize, remaining);
            logger.LogInformation(
                "DataIngest catalog page fetch. Page={Page} Take={Take} UpsertedSoFar={Upserted}",
                page,
                take,
                upserted);

            var batch = await gravity.ListProductsAsync(credentials, page, take, cancellationToken);
            pageCount = Math.Max(1, batch.PageCount);

            logger.LogInformation(
                "DataIngest catalog page result. Page={Page} PageCount={PageCount} RowCount={RowCount} Results={Results}",
                batch.CurrentPage,
                batch.PageCount,
                batch.RowCount,
                batch.Results.Count);

            var ids = batch.Results
                .Where(x => !string.IsNullOrWhiteSpace(x.ProductId))
                .Select(x => x.ProductId)
                .Distinct(StringComparer.Ordinal)
                .Take(take)
                .ToList();

            var now = DateTimeOffset.UtcNow;
            var prepared = await PrepareCatalogEntitiesAsync(
                credentials,
                partitionKey,
                ids,
                now,
                cancellationToken);

            if (prepared.Entities.Count > 0)
            {
                await tables.UpsertCatalogBatchAsync(prepared.Entities, cancellationToken);
                upserted += prepared.Entities.Count;
                logger.LogInformation(
                    "DataIngest catalog batch upsert. Count={Count} UpsertedSoFar={Upserted} VectorEnqueue={Enqueue}",
                    prepared.Entities.Count,
                    upserted,
                    prepared.ToEnqueue.Count);
            }

            foreach (var item in prepared.ToEnqueue)
            {
                try
                {
                    var message = new CatalogVectorIngestMessage(
                        tenantKey,
                        integrationId,
                        item.ProductId,
                        item.ContentHash);
                    var body = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(message, Json));
                    await queue.SendAsync(DataIngestQueues.CatalogVector, body, cancellationToken);
                    enqueued++;
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    // Tables remain pending; recovery via pending-stale on next sync.
                    logger.LogWarning(
                        ex,
                        "DataIngest catalog vector enqueue failed. ProductId={ProductId}",
                        item.ProductId);
                }
            }

            if (max.HasValue && upserted >= max.Value)
                break;

            if (batch.Results.Count == 0)
                break;

            page++;
        } while (page <= pageCount);

        logger.LogInformation(
            "DataIngest catalog sync done. Upserted={Upserted} VectorEnqueued={Enqueued}",
            upserted,
            enqueued);

        return upserted;
    }

    private async Task<(IReadOnlyList<DataIngestEntity> Entities, IReadOnlyList<(string ProductId, string ContentHash)> ToEnqueue)> PrepareCatalogEntitiesAsync(
        GravityStoreCredentials credentials,
        string partitionKey,
        IReadOnlyList<string> productIds,
        DateTimeOffset syncedAt,
        CancellationToken cancellationToken)
    {
        if (productIds.Count == 0)
            return ([], []);

        var bag = new System.Collections.Concurrent.ConcurrentBag<DataIngestEntity>();
        var enqueueBag = new System.Collections.Concurrent.ConcurrentBag<(string ProductId, string ContentHash)>();

        logger.LogInformation(
            "DataIngest catalog GetById+meta parallel. Count={Count} MaxDegree={MaxDegree}",
            productIds.Count,
            GetByIdMaxDegreeOfParallelism);

        await Parallel.ForEachAsync(
            productIds,
            new ParallelOptions
            {
                MaxDegreeOfParallelism = GetByIdMaxDegreeOfParallelism,
                CancellationToken = cancellationToken
            },
            async (productId, ct) =>
            {
                var detail = await gravity.GetProductByIdAsync(credentials, productId, ct);
                if (detail is null || string.IsNullOrWhiteSpace(detail.ProductId))
                {
                    logger.LogWarning(
                        "DataIngest catalog GetById returned empty. ProductId={ProductId}",
                        productId);
                    return;
                }

                var hash = DataIngestContentHash.Compute(detail.PayloadJson);
                var meta = await tables.GetCatalogMetaAsync(partitionKey, detail.ProductId, ct);
                var enqueue = CatalogVectorEnqueue.ShouldEnqueue(meta, hash, detail.UpdatedOn, syncedAt);

                string? pipelineStatus;
                DateTimeOffset? statusChangedAt;
                string? contentHashToStore;

                if (enqueue)
                {
                    pipelineStatus = DataIngestPipelineStatuses.Pending;
                    statusChangedAt = syncedAt;
                    contentHashToStore = hash;
                    enqueueBag.Add((detail.ProductId, hash));
                }
                else
                {
                    // Preserve indexed pipeline state; keep existing hash.
                    pipelineStatus = meta?.Status ?? DataIngestPipelineStatuses.Indexed;
                    statusChangedAt = meta?.StatusChangedAt;
                    contentHashToStore = meta?.ContentHash ?? hash;
                }

                bag.Add(new DataIngestEntity(
                    partitionKey,
                    detail.ProductId,
                    detail.Name,
                    detail.ProductStatusName,
                    pipelineStatus,
                    contentHashToStore,
                    statusChangedAt,
                    detail.UpdatedOn,
                    syncedAt,
                    detail.PayloadJson));
            });

        return (bag.ToList(), enqueueBag.ToList());
    }

    private async Task<int> SyncOrdersAsync(
        GravityStoreCredentials credentials,
        string partitionKey,
        CancellationToken cancellationToken)
    {
        var max = options.Value.EffectiveOrderLimit;
        var upserted = 0;
        var page = 1;
        bool hasMore;
        do
        {
            var remaining = max.HasValue ? max.Value - upserted : PageSize;
            if (remaining <= 0)
                break;

            var take = Math.Min(PageSize, remaining);
            logger.LogInformation(
                "DataIngest orders page fetch. Page={Page} Take={Take} UpsertedSoFar={Upserted}",
                page,
                take,
                upserted);

            var batch = await gravity.ListOrdersAsync(credentials, page, take, cancellationToken);
            hasMore = batch.HasMore;

            logger.LogInformation(
                "DataIngest orders page result. Page={Page} Results={Results} HasMore={HasMore}",
                batch.CurrentPage,
                batch.Results.Count,
                batch.HasMore);

            var ids = batch.Results
                .Where(x => !string.IsNullOrWhiteSpace(x.SaleOrderId))
                .Select(x => x.SaleOrderId)
                .Distinct(StringComparer.Ordinal)
                .Take(take)
                .ToList();

            var now = DateTimeOffset.UtcNow;
            var toUpsert = await FetchOrderDetailsParallelAsync(
                credentials,
                partitionKey,
                ids,
                now,
                cancellationToken);

            if (toUpsert.Count > 0)
            {
                await tables.UpsertOrderBatchAsync(toUpsert, cancellationToken);
                upserted += toUpsert.Count;
                logger.LogInformation(
                    "DataIngest orders batch upsert. Count={Count} UpsertedSoFar={Upserted}",
                    toUpsert.Count,
                    upserted);
            }

            if (max.HasValue && upserted >= max.Value)
                break;

            if (batch.Results.Count == 0)
                hasMore = false;

            page++;
        } while (hasMore);

        return upserted;
    }

    private async Task<IReadOnlyList<DataIngestEntity>> FetchOrderDetailsParallelAsync(
        GravityStoreCredentials credentials,
        string partitionKey,
        IReadOnlyList<string> orderIds,
        DateTimeOffset syncedAt,
        CancellationToken cancellationToken)
    {
        if (orderIds.Count == 0)
            return [];

        var bag = new System.Collections.Concurrent.ConcurrentBag<DataIngestEntity>();

        logger.LogInformation(
            "DataIngest orders GetById parallel. Count={Count} MaxDegree={MaxDegree}",
            orderIds.Count,
            GetByIdMaxDegreeOfParallelism);

        await Parallel.ForEachAsync(
            orderIds,
            new ParallelOptions
            {
                MaxDegreeOfParallelism = GetByIdMaxDegreeOfParallelism,
                CancellationToken = cancellationToken
            },
            async (orderId, ct) =>
            {
                var detail = await gravity.GetOrderByIdAsync(credentials, orderId, ct);
                if (detail is null || string.IsNullOrWhiteSpace(detail.SaleOrderId))
                {
                    logger.LogWarning(
                        "DataIngest orders GetById returned empty. OrderId={OrderId}",
                        orderId);
                    return;
                }

                bag.Add(new DataIngestEntity(
                    partitionKey,
                    detail.SaleOrderId,
                    detail.Name,
                    detail.Status,
                    null,
                    null,
                    null,
                    detail.UpdatedOn,
                    syncedAt,
                    detail.PayloadJson));
            });

        return bag.ToList();
    }
}
