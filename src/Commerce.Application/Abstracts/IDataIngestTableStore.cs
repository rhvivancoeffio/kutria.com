namespace Commerce.Application.Abstracts;

public static class DataIngestQueues
{
    public const string Ingest = "data-ingest";
    public const string CatalogVector = "catalog-vector-ingest";
    public const string CatalogImage = "catalog-image-vector";
}

public static class DataIngestKinds
{
    public const string Catalog = "catalog";
    public const string Orders = "orders";
    public const string All = "all";
}

public static class DataIngestPipelineStatuses
{
    public const string Pending = "pending";
    public const string Processing = "processing";
    public const string Indexed = "indexed";
    public const string Failed = "failed";

    public static readonly TimeSpan PendingStaleAfter = TimeSpan.FromMinutes(15);
    public static readonly TimeSpan ProcessingLease = TimeSpan.FromMinutes(10);
    public const int PoisonDequeueCount = 5;

    public static bool IsKnown(string? status) =>
        string.Equals(status, Pending, StringComparison.OrdinalIgnoreCase)
        || string.Equals(status, Processing, StringComparison.OrdinalIgnoreCase)
        || string.Equals(status, Indexed, StringComparison.OrdinalIgnoreCase)
        || string.Equals(status, Failed, StringComparison.OrdinalIgnoreCase);
}

/// <summary>
/// Per-SKU image vector lifecycle in the products index (<c>image_status</c>).
/// </summary>
public static class CatalogImageStatuses
{
    public const string None = "none";
    public const string Pending = "pending";
    public const string Indexed = "indexed";
    public const string Failed = "failed";
}

public sealed record DataIngestMessage(string Tenant, Guid IntegrationId, string Kind);

public sealed record CatalogVectorIngestMessage(
    string Tenant,
    Guid IntegrationId,
    string ProductId,
    string ExpectedContentHash);

/// <summary>
/// One message per unique image URL (tenant-scoped). <see cref="Skus"/> share that URL.
/// </summary>
public sealed record CatalogImageVectorMessage(
    string Tenant,
    Guid IntegrationId,
    string ProductId,
    string ImageUrl,
    IReadOnlyList<string> Skus,
    string ExpectedContentHash);

public sealed record DataIngestEntity(
    string PartitionKey,
    string RowKey,
    string? Name,
    string? ProductStatusName,
    string? Status,
    string? ContentHash,
    DateTimeOffset? StatusChangedAt,
    DateTimeOffset? UpdatedOn,
    DateTimeOffset SyncedAt,
    string PayloadJson);

/// <summary>
/// Large Gravity payloads live in Blob Storage; Table rows keep only meta + pointer.
/// </summary>
public interface IDataIngestPayloadBlobStore
{
    Task WriteAsync(
        string kind,
        string partitionKey,
        string rowKey,
        string payloadJson,
        CancellationToken cancellationToken = default);

    Task<string?> ReadAsync(
        string kind,
        string partitionKey,
        string rowKey,
        CancellationToken cancellationToken = default);
}

public sealed record DataIngestCatalogMeta(
    string PartitionKey,
    string RowKey,
    string? Status,
    string? ContentHash,
    DateTimeOffset? StatusChangedAt,
    DateTimeOffset? UpdatedOn);

public sealed record DataIngestListResult(
    IReadOnlyList<DataIngestEntity> Items,
    int PageSize,
    string? NextToken,
    bool HasMore);

public interface IDataIngestTableStore
{
    Task UpsertCatalogAsync(DataIngestEntity entity, CancellationToken cancellationToken = default);

    Task UpsertOrderAsync(DataIngestEntity entity, CancellationToken cancellationToken = default);

    /// <summary>
    /// Batch upsert (Azure Table transaction). Entities must share PartitionKey; chunks of ≤100.
    /// </summary>
    Task UpsertCatalogBatchAsync(
        IReadOnlyList<DataIngestEntity> entities,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Batch upsert (Azure Table transaction). Entities must share PartitionKey; chunks of ≤100.
    /// </summary>
    Task UpsertOrderBatchAsync(
        IReadOnlyList<DataIngestEntity> entities,
        CancellationToken cancellationToken = default);

    Task<DataIngestListResult> ListCatalogAsync(
        string partitionKey,
        int pageSize,
        string? nextToken,
        CancellationToken cancellationToken = default);

    Task<DataIngestEntity?> GetCatalogAsync(
        string partitionKey,
        string rowKey,
        CancellationToken cancellationToken = default);

    /// <summary>Lightweight get without PayloadJson (pipeline gate).</summary>
    Task<DataIngestCatalogMeta?> GetCatalogMetaAsync(
        string partitionKey,
        string rowKey,
        CancellationToken cancellationToken = default);

    /// <summary>Merge-update pipeline fields only; does not touch PayloadJson.</summary>
    Task PatchCatalogPipelineAsync(
        string partitionKey,
        string rowKey,
        string status,
        string? contentHash,
        DateTimeOffset statusChangedAt,
        CancellationToken cancellationToken = default);

    Task<DataIngestListResult> ListOrdersAsync(
        string partitionKey,
        int pageSize,
        string? nextToken,
        CancellationToken cancellationToken = default);

    Task<DataIngestEntity?> GetOrderAsync(
        string partitionKey,
        string rowKey,
        CancellationToken cancellationToken = default);
}

public static class DataIngestPartition
{
    public static string Key(string tenantId, Guid integrationId) => $"{tenantId}_{integrationId:D}";
}
