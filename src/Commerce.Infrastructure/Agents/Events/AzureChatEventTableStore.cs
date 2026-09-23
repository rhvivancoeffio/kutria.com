using System.Collections.Concurrent;
using System.Text;
using Azure;
using Azure.Data.Tables;
using Commerce.Application.Abstracts;
using Microsoft.Extensions.Logging;

namespace Commerce.Infrastructure.Agents.Events;

public sealed class AzureChatEventTableStore : IChatEventTableStore
{
    public const string TableName = "commercechatevents";

    private readonly TableClient _table;
    private int _ensured;

    public AzureChatEventTableStore(string connectionString, ILogger<AzureChatEventTableStore> logger)
    {
        _ = logger;
        var service = new TableServiceClient(connectionString);
        _table = service.GetTableClient(TableName);
    }

    public async Task UpsertAsync(ChatEventEntity entity, CancellationToken cancellationToken = default)
    {
        await EnsureTableAsync(cancellationToken);
        var row = ToEntity(entity);
        await _table.UpsertEntityAsync(row, TableUpdateMode.Replace, cancellationToken);
    }

    public async Task<ChatEventListResult> ListAsync(
        string tenantId,
        int pageSize,
        string? nextToken,
        string? threadId = null,
        string? eventType = null,
        bool? emptyOnly = null,
        CancellationToken cancellationToken = default)
    {
        await EnsureTableAsync(cancellationToken);
        pageSize = Math.Clamp(pageSize, 1, 100);
        if (string.IsNullOrWhiteSpace(tenantId))
            return new ChatEventListResult([], pageSize, null, false);

        var partition = ChatEventKeys.Partition(tenantId);
        var filter = TableClient.CreateQueryFilter($"PartitionKey eq {partition}");
        if (!string.IsNullOrWhiteSpace(threadId))
            filter += " and " + TableClient.CreateQueryFilter($"ThreadId eq {threadId.Trim()}");
        if (!string.IsNullOrWhiteSpace(eventType))
            filter += " and " + TableClient.CreateQueryFilter($"EventType eq {eventType.Trim()}");
        if (emptyOnly is true)
            filter += " and Empty eq true";

        var continuation = string.IsNullOrWhiteSpace(nextToken) ? null : nextToken;
        await foreach (var page in _table
            .QueryAsync<ChatEventTableEntity>(filter, maxPerPage: pageSize, cancellationToken: cancellationToken)
            .AsPages(continuation, pageSize))
        {
            var items = page.Values.Select(ToModel).ToList();
            var token = page.ContinuationToken;
            return new ChatEventListResult(
                items,
                pageSize,
                string.IsNullOrEmpty(token) ? null : token,
                !string.IsNullOrEmpty(token));
        }

        return new ChatEventListResult([], pageSize, null, false);
    }

    private async Task EnsureTableAsync(CancellationToken cancellationToken)
    {
        if (Interlocked.CompareExchange(ref _ensured, 1, 0) == 0)
            await _table.CreateIfNotExistsAsync(cancellationToken);
    }

    private static ChatEventTableEntity ToEntity(ChatEventEntity e) => new()
    {
        PartitionKey = e.PartitionKey,
        RowKey = e.RowKey,
        EventType = e.EventType,
        ThreadId = e.ThreadId,
        Audience = e.Audience,
        AgentKey = e.AgentKey,
        Tools = e.Tools,
        LatencyMs = e.LatencyMs,
        Empty = e.Empty,
        Error = e.Error,
        OccurredAt = e.OccurredAt
    };

    private static ChatEventEntity ToModel(ChatEventTableEntity e) => new(
        e.PartitionKey,
        e.RowKey,
        e.EventType ?? string.Empty,
        e.ThreadId ?? string.Empty,
        e.Audience ?? string.Empty,
        e.AgentKey,
        e.Tools ?? string.Empty,
        e.LatencyMs,
        e.Empty,
        e.Error,
        e.OccurredAt == default ? e.Timestamp ?? DateTimeOffset.UtcNow : e.OccurredAt);
}

public sealed class ChatEventTableEntity : ITableEntity
{
    public string PartitionKey { get; set; } = string.Empty;
    public string RowKey { get; set; } = string.Empty;
    public DateTimeOffset? Timestamp { get; set; }
    public ETag ETag { get; set; }
    public string? EventType { get; set; }
    public string? ThreadId { get; set; }
    public string? Audience { get; set; }
    public string? AgentKey { get; set; }
    public string? Tools { get; set; }
    public long LatencyMs { get; set; }
    public bool Empty { get; set; }
    public string? Error { get; set; }
    public DateTimeOffset OccurredAt { get; set; }
}

/// <summary>Dev fallback when Azure Tables is not configured.</summary>
public sealed class InMemoryChatEventTableStore : IChatEventTableStore
{
    private readonly ConcurrentDictionary<string, ConcurrentDictionary<string, ChatEventEntity>> _rows = new();

    public Task UpsertAsync(ChatEventEntity entity, CancellationToken cancellationToken = default)
    {
        var partition = _rows.GetOrAdd(entity.PartitionKey, _ => new(StringComparer.Ordinal));
        partition.AddOrUpdate(entity.RowKey, entity, (_, _) => entity);
        return Task.CompletedTask;
    }

    public Task<ChatEventListResult> ListAsync(
        string tenantId,
        int pageSize,
        string? nextToken,
        string? threadId = null,
        string? eventType = null,
        bool? emptyOnly = null,
        CancellationToken cancellationToken = default)
    {
        pageSize = Math.Clamp(pageSize, 1, 100);
        if (string.IsNullOrWhiteSpace(tenantId))
            return Task.FromResult(new ChatEventListResult([], pageSize, null, false));

        var partition = ChatEventKeys.Partition(tenantId);
        if (!_rows.TryGetValue(partition, out var map))
            return Task.FromResult(new ChatEventListResult([], pageSize, null, false));

        IEnumerable<ChatEventEntity> query = map.Values.OrderBy(x => x.RowKey, StringComparer.Ordinal);
        if (!string.IsNullOrWhiteSpace(threadId))
            query = query.Where(x => string.Equals(x.ThreadId, threadId.Trim(), StringComparison.Ordinal));
        if (!string.IsNullOrWhiteSpace(eventType))
            query = query.Where(x => string.Equals(x.EventType, eventType.Trim(), StringComparison.OrdinalIgnoreCase));
        if (emptyOnly is true)
            query = query.Where(x => x.Empty);

        var all = query.ToList();
        var offset = DecodeOffset(nextToken);
        var page = all.Skip(offset).Take(pageSize).ToList();
        var next = offset + page.Count;
        var hasMore = next < all.Count;
        return Task.FromResult(new ChatEventListResult(
            page,
            pageSize,
            hasMore ? EncodeOffset(next) : null,
            hasMore));
    }

    private static int DecodeOffset(string? nextToken)
    {
        if (string.IsNullOrWhiteSpace(nextToken))
            return 0;
        try
        {
            var raw = Encoding.UTF8.GetString(Convert.FromBase64String(nextToken));
            return int.TryParse(raw, out var n) && n >= 0 ? n : 0;
        }
        catch
        {
            return 0;
        }
    }

    private static string EncodeOffset(int offset)
        => Convert.ToBase64String(Encoding.UTF8.GetBytes(offset.ToString()));
}
