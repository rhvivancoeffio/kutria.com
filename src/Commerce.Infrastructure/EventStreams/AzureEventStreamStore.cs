using System.Collections.Concurrent;
using Azure;
using Azure.Data.Tables;
using Commerce.Application.Abstracts;
using Commerce.Application.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Commerce.Infrastructure.EventStreams;

public sealed class AzureEventStreamStore : IEventStreamStore
{
    public const string TableName = "commerceeventstreams";
    public const string GcTableName = "commerceeventstreamgc";

    private readonly TableClient _table;
    private readonly TableClient _gc;
    private readonly EventStreamsOptions _options;
    private readonly ILogger<AzureEventStreamStore> _logger;
    private long _seq;
    private int _ensured;

    public AzureEventStreamStore(
        string connectionString,
        IOptions<EventStreamsOptions> options,
        ILogger<AzureEventStreamStore> logger)
    {
        _options = options.Value ?? new EventStreamsOptions();
        _logger = logger;
        var service = new TableServiceClient(connectionString);
        _table = service.GetTableClient(TableName);
        _gc = service.GetTableClient(GcTableName);
    }

    public async Task<string> StartProcessAsync(
        string tenantId,
        string? kind = null,
        string? data = null,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(tenantId))
            throw new ArgumentException("tenantId is required.", nameof(tenantId));

        var processId = Guid.NewGuid().ToString("N");
        var meta = string.IsNullOrWhiteSpace(kind)
            ? data
            : (string.IsNullOrWhiteSpace(data)
                ? $"{{\"kind\":\"{EscapeJson(kind)}\"}}"
                : data);
        await AppendAsync(
            tenantId.Trim(),
            processId,
            EventStreamKeys.TypeStarted,
            kind,
            meta,
            cancellationToken);
        return processId;
    }

    private static string EscapeJson(string value)
        => value.Replace("\\", "\\\\", StringComparison.Ordinal).Replace("\"", "\\\"", StringComparison.Ordinal);

    public async Task<string> AppendAsync(
        string tenantId,
        string streamId,
        string type,
        string? text = null,
        string? data = null,
        CancellationToken cancellationToken = default)
    {
        await EnsureTableAsync(cancellationToken);
        var partition = EventStreamKeys.Partition(tenantId, streamId);
        var rowKey = EventStreamKeys.NewRowKey(Interlocked.Increment(ref _seq));
        var entity = new EventStreamTableEntity
        {
            PartitionKey = partition,
            RowKey = rowKey,
            Type = type,
            Text = text,
            Data = data
        };
        await _table.UpsertEntityAsync(entity, TableUpdateMode.Replace, cancellationToken);

        if (EventStreamKeys.IsTerminal(type))
        {
            await UpsertMetaCompletedAsync(partition, type, cancellationToken);
            await ScheduleGcAsync(tenantId, streamId, cancellationToken);
        }

        return rowKey;
    }

    public async Task<StreamEventListResult> ListSinceAsync(
        string tenantId,
        string streamId,
        string? afterRowKey,
        int take = 50,
        CancellationToken cancellationToken = default)
    {
        await EnsureTableAsync(cancellationToken);
        take = Math.Clamp(take, 1, 100);
        if (string.IsNullOrWhiteSpace(tenantId) || string.IsNullOrWhiteSpace(streamId))
            return new StreamEventListResult([], afterRowKey, false);

        var partition = EventStreamKeys.Partition(tenantId, streamId);
        var filter = TableClient.CreateQueryFilter($"PartitionKey eq {partition}");
        if (!string.IsNullOrWhiteSpace(afterRowKey)
            && !string.Equals(afterRowKey, EventStreamKeys.MetaRowKey, StringComparison.Ordinal))
            filter += " and " + TableClient.CreateQueryFilter($"RowKey gt {afterRowKey.Trim()}");

        var items = new List<StreamEvent>(take);
        await foreach (var page in _table
            .QueryAsync<EventStreamTableEntity>(filter, maxPerPage: take + 4, cancellationToken: cancellationToken)
            .AsPages(pageSizeHint: take + 4))
        {
            foreach (var row in page.Values)
            {
                if (string.Equals(row.RowKey, EventStreamKeys.MetaRowKey, StringComparison.Ordinal))
                    continue;
                items.Add(ToModel(row));
                if (items.Count >= take)
                    break;
            }
            break;
        }

        var nextAfter = items.Count > 0 ? items[^1].RowKey : afterRowKey;
        var completed = items.Any(x => EventStreamKeys.IsTerminal(x.Type))
            || await IsMetaCompletedAsync(partition, cancellationToken);
        return new StreamEventListResult(items, nextAfter, completed);
    }

    public async Task DeleteStreamAsync(
        string tenantId,
        string streamId,
        CancellationToken cancellationToken = default)
    {
        await EnsureTableAsync(cancellationToken);
        if (string.IsNullOrWhiteSpace(tenantId) || string.IsNullOrWhiteSpace(streamId))
            return;

        var partition = EventStreamKeys.Partition(tenantId, streamId);
        var filter = TableClient.CreateQueryFilter($"PartitionKey eq {partition}");
        await foreach (var page in _table
            .QueryAsync<TableEntity>(filter, select: ["PartitionKey", "RowKey"], cancellationToken: cancellationToken)
            .AsPages())
        {
            foreach (var row in page.Values)
            {
                try
                {
                    await _table.DeleteEntityAsync(row.PartitionKey, row.RowKey, cancellationToken: cancellationToken);
                }
                catch (RequestFailedException ex)
                {
                    _logger.LogDebug(ex, "Event stream delete skipped {Partition}/{Row}", row.PartitionKey, row.RowKey);
                }
            }
        }

        try
        {
            await _gc.DeleteEntityAsync(GcPartitionKey(DateTimeOffset.UtcNow), partition, cancellationToken: cancellationToken);
        }
        catch (RequestFailedException)
        {
            // GC row may already be gone or use another day partition.
        }
    }

    public async Task<IReadOnlyList<(string TenantId, string StreamId)>> ListDueForDeleteAsync(
        DateTimeOffset utcNow,
        int take = 50,
        CancellationToken cancellationToken = default)
    {
        await EnsureTableAsync(cancellationToken);
        take = Math.Clamp(take, 1, 100);
        var due = new List<(string, string)>(take);
        // Scan today and a few prior day buckets (TTL scheduling uses delete-after day).
        for (var day = 0; day < 4 && due.Count < take; day++)
        {
            var pk = GcPartitionKey(utcNow.AddDays(-day));
            var filter = TableClient.CreateQueryFilter($"PartitionKey eq {pk}");
            await foreach (var page in _gc
                .QueryAsync<EventStreamGcEntity>(filter, maxPerPage: take, cancellationToken: cancellationToken)
                .AsPages(pageSizeHint: take))
            {
                foreach (var row in page.Values)
                {
                    if (row.DeleteAfterUtc > utcNow)
                        continue;
                    if (EventStreamKeys.TrySplitPartition(row.RowKey, out var tenantId, out var streamId))
                        due.Add((tenantId, streamId));
                    if (due.Count >= take)
                        break;
                }
                break;
            }
        }

        return due;
    }

    private async Task UpsertMetaCompletedAsync(string partition, string type, CancellationToken cancellationToken)
    {
        var meta = new EventStreamTableEntity
        {
            PartitionKey = partition,
            RowKey = EventStreamKeys.MetaRowKey,
            Type = type,
            Text = "completed",
            Data = null
        };
        await _table.UpsertEntityAsync(meta, TableUpdateMode.Replace, cancellationToken);
    }

    private async Task<bool> IsMetaCompletedAsync(string partition, CancellationToken cancellationToken)
    {
        try
        {
            var meta = await _table.GetEntityAsync<EventStreamTableEntity>(
                partition,
                EventStreamKeys.MetaRowKey,
                cancellationToken: cancellationToken);
            return EventStreamKeys.IsTerminal(meta.Value.Type);
        }
        catch (RequestFailedException ex) when (ex.Status == 404)
        {
            return false;
        }
    }

    private async Task ScheduleGcAsync(string tenantId, string streamId, CancellationToken cancellationToken)
    {
        if (!TimeSpan.TryParse(_options.Ttl, out var ttl) || ttl <= TimeSpan.Zero)
            ttl = TimeSpan.FromHours(24);
        var deleteAfter = DateTimeOffset.UtcNow.Add(ttl);
        var partition = EventStreamKeys.Partition(tenantId, streamId);
        var entity = new EventStreamGcEntity
        {
            PartitionKey = GcPartitionKey(deleteAfter),
            RowKey = partition,
            DeleteAfterUtc = deleteAfter
        };
        await _gc.UpsertEntityAsync(entity, TableUpdateMode.Replace, cancellationToken);
    }

    /// <summary>Bucket GC rows by UTC day of delete-after so the worker scans one partition.</summary>
    private static string GcPartitionKey(DateTimeOffset when)
        => when.UtcDateTime.ToString("yyyyMMdd");

    private async Task EnsureTableAsync(CancellationToken cancellationToken)
    {
        if (Interlocked.CompareExchange(ref _ensured, 1, 0) == 0)
        {
            await _table.CreateIfNotExistsAsync(cancellationToken);
            await _gc.CreateIfNotExistsAsync(cancellationToken);
        }
    }

    private static StreamEvent ToModel(EventStreamTableEntity e) => new(
        e.Type ?? string.Empty,
        e.Text,
        e.Data,
        e.RowKey);
}

public sealed class EventStreamTableEntity : ITableEntity
{
    public string PartitionKey { get; set; } = string.Empty;
    public string RowKey { get; set; } = string.Empty;
    public DateTimeOffset? Timestamp { get; set; }
    public ETag ETag { get; set; }
    public string? Type { get; set; }
    public string? Text { get; set; }
    public string? Data { get; set; }
}

public sealed class EventStreamGcEntity : ITableEntity
{
    public string PartitionKey { get; set; } = string.Empty;
    public string RowKey { get; set; } = string.Empty;
    public DateTimeOffset? Timestamp { get; set; }
    public ETag ETag { get; set; }
    public DateTimeOffset DeleteAfterUtc { get; set; }
}

/// <summary>Dev fallback when Azure Tables is not configured.</summary>
public sealed class InMemoryEventStreamStore : IEventStreamStore
{
    private readonly ConcurrentDictionary<string, ConcurrentDictionary<string, StreamEvent>> _rows = new();
    private readonly ConcurrentDictionary<string, DateTimeOffset> _gc = new(StringComparer.Ordinal);
    private readonly EventStreamsOptions _options;
    private long _seq;

    public InMemoryEventStreamStore(IOptions<EventStreamsOptions>? options = null)
    {
        _options = options?.Value ?? new EventStreamsOptions();
    }

    public async Task<string> StartProcessAsync(
        string tenantId,
        string? kind = null,
        string? data = null,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(tenantId))
            throw new ArgumentException("tenantId is required.", nameof(tenantId));

        var processId = Guid.NewGuid().ToString("N");
        var meta = string.IsNullOrWhiteSpace(kind)
            ? data
            : (string.IsNullOrWhiteSpace(data)
                ? $"{{\"kind\":\"{kind.Replace("\\", "\\\\", StringComparison.Ordinal).Replace("\"", "\\\"", StringComparison.Ordinal)}\"}}"
                : data);
        await AppendAsync(
            tenantId.Trim(),
            processId,
            EventStreamKeys.TypeStarted,
            kind,
            meta,
            cancellationToken);
        return processId;
    }

    public Task<string> AppendAsync(
        string tenantId,
        string streamId,
        string type,
        string? text = null,
        string? data = null,
        CancellationToken cancellationToken = default)
    {
        var partition = EventStreamKeys.Partition(tenantId, streamId);
        var rowKey = EventStreamKeys.NewRowKey(Interlocked.Increment(ref _seq));
        var evt = new StreamEvent(type, text, data, rowKey);
        var map = _rows.GetOrAdd(partition, _ => new(StringComparer.Ordinal));
        map[rowKey] = evt;
        if (EventStreamKeys.IsTerminal(type))
        {
            map[EventStreamKeys.MetaRowKey] = new StreamEvent(type, "completed", null, EventStreamKeys.MetaRowKey);
            if (!TimeSpan.TryParse(_options.Ttl, out var ttl) || ttl <= TimeSpan.Zero)
                ttl = TimeSpan.FromHours(24);
            _gc[partition] = DateTimeOffset.UtcNow.Add(ttl);
        }

        return Task.FromResult(rowKey);
    }

    public Task<StreamEventListResult> ListSinceAsync(
        string tenantId,
        string streamId,
        string? afterRowKey,
        int take = 50,
        CancellationToken cancellationToken = default)
    {
        take = Math.Clamp(take, 1, 100);
        if (string.IsNullOrWhiteSpace(tenantId) || string.IsNullOrWhiteSpace(streamId))
            return Task.FromResult(new StreamEventListResult([], afterRowKey, false));

        var partition = EventStreamKeys.Partition(tenantId, streamId);
        if (!_rows.TryGetValue(partition, out var map))
            return Task.FromResult(new StreamEventListResult([], afterRowKey, false));

        IEnumerable<StreamEvent> query = map.Values
            .Where(x => !string.Equals(x.RowKey, EventStreamKeys.MetaRowKey, StringComparison.Ordinal))
            .OrderBy(x => x.RowKey, StringComparer.Ordinal);
        if (!string.IsNullOrWhiteSpace(afterRowKey))
            query = query.Where(x => string.CompareOrdinal(x.RowKey, afterRowKey.Trim()) > 0);

        var items = query.Take(take).ToList();
        var nextAfter = items.Count > 0 ? items[^1].RowKey : afterRowKey;
        var completed = items.Any(x => EventStreamKeys.IsTerminal(x.Type))
            || (map.TryGetValue(EventStreamKeys.MetaRowKey, out var meta) && EventStreamKeys.IsTerminal(meta.Type));
        return Task.FromResult(new StreamEventListResult(items, nextAfter, completed));
    }

    public Task DeleteStreamAsync(
        string tenantId,
        string streamId,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(tenantId) || string.IsNullOrWhiteSpace(streamId))
            return Task.CompletedTask;
        var partition = EventStreamKeys.Partition(tenantId, streamId);
        _rows.TryRemove(partition, out _);
        _gc.TryRemove(partition, out _);
        return Task.CompletedTask;
    }

    public Task<IReadOnlyList<(string TenantId, string StreamId)>> ListDueForDeleteAsync(
        DateTimeOffset utcNow,
        int take = 50,
        CancellationToken cancellationToken = default)
    {
        take = Math.Clamp(take, 1, 100);
        var due = new List<(string, string)>();
        foreach (var (partition, when) in _gc)
        {
            if (when > utcNow)
                continue;
            if (EventStreamKeys.TrySplitPartition(partition, out var tenantId, out var streamId))
                due.Add((tenantId, streamId));
            if (due.Count >= take)
                break;
        }

        return Task.FromResult<IReadOnlyList<(string, string)>>(due);
    }
}
