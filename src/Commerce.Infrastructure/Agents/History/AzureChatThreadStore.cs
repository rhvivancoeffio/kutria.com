using System.Text.Json;
using Azure;
using Azure.Data.Tables;
using Commerce.Application.Abstracts;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Commerce.Infrastructure.Agents.History;

/// <summary>
/// Conversation history in Azure Table Storage. One entity per thread (JSON message list).
/// PartitionKey = tenantId, RowKey = {audience}|{threadId} — list queries the partition by audience prefix.
/// </summary>
public sealed class AzureChatThreadStore : IChatThreadStore
{
    public const string TableName = "commercechatthreads";
    public const int DefaultMaxMessages = 40;

    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    private readonly TableClient _table;
    private readonly ILogger<AzureChatThreadStore> _logger;
    private readonly int _maxMessages;
    private int _ensured;

    public AzureChatThreadStore(
        string connectionString,
        IConfiguration configuration,
        ILogger<AzureChatThreadStore> logger)
    {
        _logger = logger;
        _maxMessages = int.TryParse(configuration["Chat:HistoryMaxMessages"], out var max) && max > 0
            ? max
            : DefaultMaxMessages;
        var service = new TableServiceClient(connectionString);
        _table = service.GetTableClient(TableName);
    }

    public async Task<IReadOnlyList<ChatThreadMessage>> GetAsync(
        string tenantId,
        string audience,
        string threadId,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(tenantId)
            || string.IsNullOrWhiteSpace(audience)
            || string.IsNullOrWhiteSpace(threadId))
            return [];

        await EnsureTableAsync(cancellationToken);
        try
        {
            var response = await _table.GetEntityAsync<ChatThreadTableEntity>(
                Partition(tenantId),
                RowKey(audience, threadId),
                cancellationToken: cancellationToken);
            return Deserialize(response.Value.MessagesJson);
        }
        catch (RequestFailedException ex) when (ex.Status == 404)
        {
            return [];
        }
        catch (JsonException ex)
        {
            _logger.LogWarning(
                ex,
                "Chat history ignored malformed payload for tenant {TenantId} thread {ThreadId}",
                tenantId,
                threadId);
            return [];
        }
    }

    public async Task SaveAsync(
        string tenantId,
        string audience,
        string threadId,
        IReadOnlyList<ChatThreadMessage> messages,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(tenantId)
            || string.IsNullOrWhiteSpace(audience)
            || string.IsNullOrWhiteSpace(threadId))
            throw new ArgumentException("tenantId, audience, and threadId are required.");

        await EnsureTableAsync(cancellationToken);
        var trimmed = messages.Count <= _maxMessages
            ? messages.ToList()
            : messages.Skip(messages.Count - _maxMessages).ToList();

        var entity = new ChatThreadTableEntity
        {
            PartitionKey = Partition(tenantId),
            RowKey = RowKey(audience, threadId),
            Audience = audience.Trim(),
            ThreadId = threadId.Trim(),
            Title = ChatThreadTitles.FromMessages(trimmed),
            MessagesJson = JsonSerializer.Serialize(trimmed, Json),
            UpdatedAt = DateTimeOffset.UtcNow
        };
        await _table.UpsertEntityAsync(entity, TableUpdateMode.Replace, cancellationToken);
    }

    public async Task<IReadOnlyList<ChatThreadSummary>> ListAsync(
        string tenantId,
        string audience,
        int take = 50,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(tenantId) || string.IsNullOrWhiteSpace(audience))
            return [];

        take = Math.Clamp(take, 1, 100);
        await EnsureTableAsync(cancellationToken);

        var partition = Partition(tenantId);
        var prefix = $"{audience.Trim()}|";
        var filter = TableClient.CreateQueryFilter($"PartitionKey eq {partition}");
        var results = new List<ChatThreadSummary>();

        await foreach (var entity in _table.QueryAsync<ChatThreadTableEntity>(
                           filter: filter,
                           cancellationToken: cancellationToken))
        {
            if (entity.RowKey is null || !entity.RowKey.StartsWith(prefix, StringComparison.Ordinal))
                continue;

            var threadId = string.IsNullOrWhiteSpace(entity.ThreadId)
                ? entity.RowKey[prefix.Length..]
                : entity.ThreadId;
            if (string.IsNullOrWhiteSpace(threadId))
                continue;

            var messages = Deserialize(entity.MessagesJson);
            results.Add(new ChatThreadSummary(
                threadId,
                string.IsNullOrWhiteSpace(entity.Title) ? ChatThreadTitles.FromMessages(messages) : entity.Title,
                entity.UpdatedAt == default ? entity.Timestamp ?? DateTimeOffset.UtcNow : entity.UpdatedAt,
                messages.Count));
        }

        return results
            .OrderByDescending(x => x.UpdatedAt)
            .Take(take)
            .ToList();
    }

    private async Task EnsureTableAsync(CancellationToken cancellationToken)
    {
        if (Interlocked.CompareExchange(ref _ensured, 1, 0) == 0)
            await _table.CreateIfNotExistsAsync(cancellationToken);
    }

    private static string Partition(string tenantId) => tenantId.Trim();

    private static string RowKey(string audience, string threadId)
        => $"{audience.Trim()}|{threadId.Trim()}";

    private static IReadOnlyList<ChatThreadMessage> Deserialize(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
            return [];
        return JsonSerializer.Deserialize<List<ChatThreadMessage>>(json, Json) ?? [];
    }
}

public sealed class ChatThreadTableEntity : ITableEntity
{
    public string PartitionKey { get; set; } = string.Empty;
    public string RowKey { get; set; } = string.Empty;
    public DateTimeOffset? Timestamp { get; set; }
    public ETag ETag { get; set; }
    public string? Audience { get; set; }
    public string? ThreadId { get; set; }
    public string? Title { get; set; }
    public string? MessagesJson { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
}
