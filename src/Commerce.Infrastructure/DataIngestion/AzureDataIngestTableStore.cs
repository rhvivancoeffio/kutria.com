using System.Collections.Concurrent;
using System.Text;
using Azure;
using Azure.Data.Tables;
using Commerce.Application.Abstracts;
using Microsoft.Extensions.Logging;

namespace Commerce.Infrastructure.DataIngestion;

public sealed class AzureDataIngestTableStore : IDataIngestTableStore
{
    public const string CatalogTableName = "commercecatalog";
    public const string OrdersTableName = "commerceorders";
    public const string CatalogKind = "catalog";
    public const string OrdersKind = "orders";

    private static readonly string[] MetaSelect =
    [
        "PartitionKey",
        "RowKey",
        "Status",
        "ProductStatusName",
        "ContentHash",
        "StatusChangedAt",
        "UpdatedOn"
    ];

    private readonly TableClient _catalog;
    private readonly TableClient _orders;
    private readonly IDataIngestPayloadBlobStore _payloads;
    private readonly ILogger<AzureDataIngestTableStore> _logger;
    private int _ensured;

    public AzureDataIngestTableStore(
        string connectionString,
        IDataIngestPayloadBlobStore payloads,
        ILogger<AzureDataIngestTableStore> logger)
    {
        _payloads = payloads;
        _logger = logger;
        var service = new TableServiceClient(connectionString);
        _catalog = service.GetTableClient(CatalogTableName);
        _orders = service.GetTableClient(OrdersTableName);
    }

    public Task UpsertCatalogAsync(DataIngestEntity entity, CancellationToken cancellationToken = default)
        => UpsertBatchAsync(_catalog, CatalogKind, [entity], cancellationToken);

    public Task UpsertOrderAsync(DataIngestEntity entity, CancellationToken cancellationToken = default)
        => UpsertBatchAsync(_orders, OrdersKind, [entity], cancellationToken);

    public Task UpsertCatalogBatchAsync(
        IReadOnlyList<DataIngestEntity> entities,
        CancellationToken cancellationToken = default)
        => UpsertBatchAsync(_catalog, CatalogKind, entities, cancellationToken);

    public Task UpsertOrderBatchAsync(
        IReadOnlyList<DataIngestEntity> entities,
        CancellationToken cancellationToken = default)
        => UpsertBatchAsync(_orders, OrdersKind, entities, cancellationToken);

    public Task<DataIngestListResult> ListCatalogAsync(
        string partitionKey,
        int pageSize,
        string? nextToken,
        CancellationToken cancellationToken = default)
        => ListAsync(_catalog, CatalogKind, partitionKey, pageSize, nextToken, cancellationToken);

    public Task<DataIngestEntity?> GetCatalogAsync(
        string partitionKey,
        string rowKey,
        CancellationToken cancellationToken = default)
        => GetAsync(_catalog, CatalogKind, partitionKey, rowKey, cancellationToken);

    public async Task<DataIngestCatalogMeta?> GetCatalogMetaAsync(
        string partitionKey,
        string rowKey,
        CancellationToken cancellationToken = default)
    {
        await EnsureTablesAsync(cancellationToken);
        try
        {
            var response = await _catalog.GetEntityAsync<DataIngestTableEntity>(
                partitionKey,
                rowKey,
                select: MetaSelect,
                cancellationToken: cancellationToken);
            return ToMeta(response.Value);
        }
        catch (RequestFailedException ex) when (ex.Status == 404)
        {
            return null;
        }
    }

    public async Task PatchCatalogPipelineAsync(
        string partitionKey,
        string rowKey,
        string status,
        string? contentHash,
        DateTimeOffset statusChangedAt,
        CancellationToken cancellationToken = default)
    {
        await EnsureTablesAsync(cancellationToken);
        var patch = new DataIngestTableEntity
        {
            PartitionKey = partitionKey,
            RowKey = rowKey,
            Status = status,
            ContentHash = contentHash,
            StatusChangedAt = statusChangedAt,
            ETag = ETag.All
        };
        await _catalog.UpdateEntityAsync(patch, ETag.All, TableUpdateMode.Merge, cancellationToken);
    }

    public Task<DataIngestListResult> ListOrdersAsync(
        string partitionKey,
        int pageSize,
        string? nextToken,
        CancellationToken cancellationToken = default)
        => ListAsync(_orders, OrdersKind, partitionKey, pageSize, nextToken, cancellationToken);

    public Task<DataIngestEntity?> GetOrderAsync(
        string partitionKey,
        string rowKey,
        CancellationToken cancellationToken = default)
        => GetAsync(_orders, OrdersKind, partitionKey, rowKey, cancellationToken);

    private async Task UpsertBatchAsync(
        TableClient table,
        string kind,
        IReadOnlyList<DataIngestEntity> entities,
        CancellationToken cancellationToken)
    {
        if (entities.Count == 0)
            return;

        await EnsureTablesAsync(cancellationToken);

        // Blob first so Table never points at a missing payload.
        await Parallel.ForEachAsync(
            entities,
            new ParallelOptions { MaxDegreeOfParallelism = 8, CancellationToken = cancellationToken },
            async (entity, ct) =>
            {
                await _payloads.WriteAsync(kind, entity.PartitionKey, entity.RowKey, entity.PayloadJson, ct);
            });

        const int maxBatch = 100;
        foreach (var byPartition in entities.GroupBy(e => e.PartitionKey, StringComparer.Ordinal))
        {
            var partitionEntities = byPartition.ToList();
            for (var offset = 0; offset < partitionEntities.Count; offset += maxBatch)
            {
                var chunk = partitionEntities.Skip(offset).Take(maxBatch).ToList();
                var actions = chunk.Select(entity =>
                {
                    var blobPath = AzureDataIngestPayloadBlobStore.BlobPath(kind, entity.PartitionKey, entity.RowKey);
                    var row = new DataIngestTableEntity
                    {
                        PartitionKey = entity.PartitionKey,
                        RowKey = entity.RowKey,
                        Name = entity.Name,
                        ProductStatusName = entity.ProductStatusName,
                        Status = entity.Status,
                        ContentHash = entity.ContentHash,
                        StatusChangedAt = entity.StatusChangedAt,
                        UpdatedOn = entity.UpdatedOn,
                        SyncedAt = entity.SyncedAt,
                        PayloadBlobPath = blobPath,
                        // Keep table under Azure property size limits; payload lives in blob.
                        PayloadJson = null
                    };
                    return new TableTransactionAction(TableTransactionActionType.UpsertReplace, row);
                }).ToList();

                await table.SubmitTransactionAsync(actions, cancellationToken);
            }
        }
    }

    private async Task<DataIngestEntity?> GetAsync(
        TableClient table,
        string kind,
        string partitionKey,
        string rowKey,
        CancellationToken cancellationToken)
    {
        await EnsureTablesAsync(cancellationToken);
        try
        {
            var response = await table.GetEntityAsync<DataIngestTableEntity>(
                partitionKey,
                rowKey,
                cancellationToken: cancellationToken);
            return await ToModelAsync(kind, response.Value, cancellationToken);
        }
        catch (RequestFailedException ex) when (ex.Status == 404)
        {
            return null;
        }
    }

    private async Task<DataIngestListResult> ListAsync(
        TableClient table,
        string kind,
        string partitionKey,
        int pageSize,
        string? nextToken,
        CancellationToken cancellationToken)
    {
        await EnsureTablesAsync(cancellationToken);
        pageSize = Math.Clamp(pageSize, 1, 100);
        var filter = TableClient.CreateQueryFilter($"PartitionKey eq {partitionKey}");
        var continuation = string.IsNullOrWhiteSpace(nextToken) ? null : nextToken;

        await foreach (var page in table
            .QueryAsync<DataIngestTableEntity>(filter, maxPerPage: pageSize, cancellationToken: cancellationToken)
            .AsPages(continuation, pageSize))
        {
            var items = new DataIngestEntity[page.Values.Count];
            await Parallel.ForEachAsync(
                Enumerable.Range(0, page.Values.Count),
                new ParallelOptions { MaxDegreeOfParallelism = 8, CancellationToken = cancellationToken },
                async (i, ct) =>
                {
                    items[i] = await ToModelAsync(kind, page.Values[i], ct);
                });

            var token = page.ContinuationToken;
            return new DataIngestListResult(
                items,
                pageSize,
                string.IsNullOrEmpty(token) ? null : token,
                !string.IsNullOrEmpty(token));
        }

        return new DataIngestListResult([], pageSize, null, false);
    }

    private async Task<DataIngestEntity> ToModelAsync(
        string kind,
        DataIngestTableEntity e,
        CancellationToken cancellationToken)
    {
        var (productStatus, pipelineStatus) = NormalizeStatuses(e.ProductStatusName, e.Status);
        var payload = await ResolvePayloadAsync(kind, e, cancellationToken);
        return new(
            e.PartitionKey,
            e.RowKey,
            e.Name,
            productStatus,
            pipelineStatus,
            e.ContentHash,
            e.StatusChangedAt,
            e.UpdatedOn,
            e.SyncedAt == default ? DateTimeOffset.UtcNow : e.SyncedAt,
            payload);
    }

    private async Task<string> ResolvePayloadAsync(
        string kind,
        DataIngestTableEntity e,
        CancellationToken cancellationToken)
    {
        // Prefer blob when pointer exists (new writes). Fall back to legacy inline PayloadJson.
        if (!string.IsNullOrWhiteSpace(e.PayloadBlobPath)
            || string.IsNullOrWhiteSpace(e.PayloadJson)
            || e.PayloadJson == "{}")
        {
            var fromBlob = await _payloads.ReadAsync(kind, e.PartitionKey, e.RowKey, cancellationToken);
            if (!string.IsNullOrWhiteSpace(fromBlob))
                return fromBlob;
        }

        if (!string.IsNullOrWhiteSpace(e.PayloadJson))
            return e.PayloadJson;

        _logger.LogWarning(
            "DataIngest payload missing. Kind={Kind} Partition={Partition} RowKey={RowKey} BlobPath={BlobPath}",
            kind,
            e.PartitionKey,
            e.RowKey,
            e.PayloadBlobPath);
        return "{}";
    }

    private static DataIngestCatalogMeta ToMeta(DataIngestTableEntity e)
    {
        var (_, pipelineStatus) = NormalizeStatuses(e.ProductStatusName, e.Status);
        return new(
            e.PartitionKey,
            e.RowKey,
            pipelineStatus,
            e.ContentHash,
            e.StatusChangedAt,
            e.UpdatedOn);
    }

    /// <summary>
    /// Legacy rows stored Gravity product status in Status. Move to ProductStatusName when Status is not a pipeline value.
    /// </summary>
    private static (string? ProductStatus, string? PipelineStatus) NormalizeStatuses(string? productStatusName, string? status)
    {
        if (!string.IsNullOrWhiteSpace(productStatusName))
            return (productStatusName, DataIngestPipelineStatuses.IsKnown(status) ? status : null);

        if (DataIngestPipelineStatuses.IsKnown(status))
            return (null, status);

        return (status, null);
    }

    private async Task EnsureTablesAsync(CancellationToken cancellationToken)
    {
        if (Interlocked.CompareExchange(ref _ensured, 1, 0) == 0)
        {
            await _catalog.CreateIfNotExistsAsync(cancellationToken);
            await _orders.CreateIfNotExistsAsync(cancellationToken);
        }
    }
}

public sealed class DataIngestTableEntity : ITableEntity
{
    public string PartitionKey { get; set; } = string.Empty;
    public string RowKey { get; set; } = string.Empty;
    public DateTimeOffset? Timestamp { get; set; }
    public ETag ETag { get; set; }
    public string? Name { get; set; }
    public string? ProductStatusName { get; set; }
    public string? Status { get; set; }
    public string? ContentHash { get; set; }
    public DateTimeOffset? StatusChangedAt { get; set; }
    public DateTimeOffset? UpdatedOn { get; set; }
    public DateTimeOffset SyncedAt { get; set; }
    /// <summary>Legacy inline payload (pre-blob). New writes leave this null.</summary>
    public string? PayloadJson { get; set; }
    /// <summary>Blob path under commercedataingest container.</summary>
    public string? PayloadBlobPath { get; set; }
}

/// <summary>Dev fallback when Azure Tables is not configured.</summary>
public sealed class InMemoryDataIngestTableStore : IDataIngestTableStore
{
    private readonly ConcurrentDictionary<string, ConcurrentDictionary<string, DataIngestEntity>> _catalog = new();
    private readonly ConcurrentDictionary<string, ConcurrentDictionary<string, DataIngestEntity>> _orders = new();

    public Task UpsertCatalogAsync(DataIngestEntity entity, CancellationToken cancellationToken = default)
    {
        _catalog.GetOrAdd(entity.PartitionKey, _ => new()).AddOrUpdate(entity.RowKey, entity, (_, _) => entity);
        return Task.CompletedTask;
    }

    public Task UpsertOrderAsync(DataIngestEntity entity, CancellationToken cancellationToken = default)
    {
        _orders.GetOrAdd(entity.PartitionKey, _ => new()).AddOrUpdate(entity.RowKey, entity, (_, _) => entity);
        return Task.CompletedTask;
    }

    public async Task UpsertCatalogBatchAsync(
        IReadOnlyList<DataIngestEntity> entities,
        CancellationToken cancellationToken = default)
    {
        foreach (var entity in entities)
            await UpsertCatalogAsync(entity, cancellationToken);
    }

    public async Task UpsertOrderBatchAsync(
        IReadOnlyList<DataIngestEntity> entities,
        CancellationToken cancellationToken = default)
    {
        foreach (var entity in entities)
            await UpsertOrderAsync(entity, cancellationToken);
    }

    public Task<DataIngestListResult> ListCatalogAsync(
        string partitionKey,
        int pageSize,
        string? nextToken,
        CancellationToken cancellationToken = default)
        => Task.FromResult(List(_catalog, partitionKey, pageSize, nextToken));

    public Task<DataIngestEntity?> GetCatalogAsync(
        string partitionKey,
        string rowKey,
        CancellationToken cancellationToken = default)
        => Task.FromResult(Get(_catalog, partitionKey, rowKey));

    public Task<DataIngestCatalogMeta?> GetCatalogMetaAsync(
        string partitionKey,
        string rowKey,
        CancellationToken cancellationToken = default)
    {
        var entity = Get(_catalog, partitionKey, rowKey);
        if (entity is null)
            return Task.FromResult<DataIngestCatalogMeta?>(null);

        return Task.FromResult<DataIngestCatalogMeta?>(new DataIngestCatalogMeta(
            entity.PartitionKey,
            entity.RowKey,
            entity.Status,
            entity.ContentHash,
            entity.StatusChangedAt,
            entity.UpdatedOn));
    }

    public Task PatchCatalogPipelineAsync(
        string partitionKey,
        string rowKey,
        string status,
        string? contentHash,
        DateTimeOffset statusChangedAt,
        CancellationToken cancellationToken = default)
    {
        var rows = _catalog.GetOrAdd(partitionKey, _ => new());
        if (!rows.TryGetValue(rowKey, out var existing))
            return Task.CompletedTask;

        rows[rowKey] = existing with
        {
            Status = status,
            ContentHash = contentHash ?? existing.ContentHash,
            StatusChangedAt = statusChangedAt
        };
        return Task.CompletedTask;
    }

    public Task<DataIngestListResult> ListOrdersAsync(
        string partitionKey,
        int pageSize,
        string? nextToken,
        CancellationToken cancellationToken = default)
        => Task.FromResult(List(_orders, partitionKey, pageSize, nextToken));

    public Task<DataIngestEntity?> GetOrderAsync(
        string partitionKey,
        string rowKey,
        CancellationToken cancellationToken = default)
        => Task.FromResult(Get(_orders, partitionKey, rowKey));

    private static DataIngestEntity? Get(
        ConcurrentDictionary<string, ConcurrentDictionary<string, DataIngestEntity>> store,
        string partitionKey,
        string rowKey)
        => store.TryGetValue(partitionKey, out var rows) && rows.TryGetValue(rowKey, out var entity) ? entity : null;

    private static DataIngestListResult List(
        ConcurrentDictionary<string, ConcurrentDictionary<string, DataIngestEntity>> store,
        string partitionKey,
        int pageSize,
        string? nextToken)
    {
        pageSize = Math.Clamp(pageSize, 1, 100);
        var rows = store.TryGetValue(partitionKey, out var map)
            ? map.Values.OrderBy(x => x.RowKey, StringComparer.Ordinal).ToList()
            : [];

        var offset = 0;
        if (!string.IsNullOrWhiteSpace(nextToken)
            && TryDecodeOffset(nextToken, out var parsed)
            && parsed >= 0)
        {
            offset = parsed;
        }

        var page = rows.Skip(offset).Take(pageSize).ToList();
        var next = offset + page.Count;
        var hasMore = next < rows.Count;
        return new DataIngestListResult(
            page,
            pageSize,
            hasMore ? EncodeOffset(next) : null,
            hasMore);
    }

    private static bool TryDecodeOffset(string nextToken, out int offset)
    {
        offset = 0;
        try
        {
            var raw = Encoding.UTF8.GetString(Convert.FromBase64String(nextToken));
            if (!raw.StartsWith("o:", StringComparison.Ordinal))
                return false;
            return int.TryParse(raw[2..], out offset);
        }
        catch
        {
            return false;
        }
    }

    private static string EncodeOffset(int offset)
        => Convert.ToBase64String(Encoding.UTF8.GetBytes($"o:{offset}"));
}
