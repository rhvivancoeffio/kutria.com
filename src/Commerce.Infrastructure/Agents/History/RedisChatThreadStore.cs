using System.Text.Json;
using Commerce.Application.Abstracts;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;

namespace Commerce.Infrastructure.Agents.History;

public sealed class RedisChatThreadStore : IChatThreadStore, IDisposable
{
    public const int DefaultMaxMessages = 40;
    public static readonly TimeSpan DefaultTtl = TimeSpan.FromHours(24);

    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    private readonly IConnectionMultiplexer? _mux;
    private readonly IDatabase? _db;
    private readonly ILogger<RedisChatThreadStore> _logger;
    private readonly TimeSpan _ttl;
    private readonly int _maxMessages;
    private readonly Dictionary<string, List<ChatThreadMessage>> _memory = new(StringComparer.Ordinal);
    private readonly Dictionary<string, List<ChatThreadSummary>> _memoryIndex = new(StringComparer.Ordinal);

    public RedisChatThreadStore(IConfiguration configuration, ILogger<RedisChatThreadStore> logger)
    {
        _logger = logger;
        _ttl = TimeSpan.TryParse(configuration["Chat:HistoryTtl"], out var ttl) && ttl > TimeSpan.Zero
            ? ttl
            : DefaultTtl;
        _maxMessages = int.TryParse(configuration["Chat:HistoryMaxMessages"], out var max) && max > 0
            ? max
            : DefaultMaxMessages;

        var redis = configuration.GetConnectionString("Redis");
        if (string.IsNullOrWhiteSpace(redis))
        {
            _logger.LogWarning("Chat history using in-process memory. Redis connection is missing.");
            return;
        }

        try
        {
            _mux = ConnectionMultiplexer.Connect(redis);
            _db = _mux.GetDatabase();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Chat history falling back to in-process memory. Redis connect failed.");
        }
    }

    public async Task<IReadOnlyList<ChatThreadMessage>> GetAsync(
        string tenantId,
        string audience,
        string threadId,
        CancellationToken cancellationToken = default)
    {
        var key = ChatThreadKeys.Build(tenantId, audience, threadId);
        if (_db is null)
        {
            return _memory.TryGetValue(key, out var local) ? local.ToList() : [];
        }

        var value = await _db.StringGetAsync(key).ConfigureAwait(false);
        if (value.IsNullOrEmpty)
        {
            return [];
        }

        try
        {
            return JsonSerializer.Deserialize<List<ChatThreadMessage>>((string)value!, Json) ?? [];
        }
        catch (JsonException ex)
        {
            _logger.LogWarning(ex, "Chat history ignored malformed payload for {Key}", key);
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
        var key = ChatThreadKeys.Build(tenantId, audience, threadId);
        var trimmed = messages.Count <= _maxMessages
            ? messages.ToList()
            : messages.Skip(messages.Count - _maxMessages).ToList();
        var payload = JsonSerializer.Serialize(trimmed, Json);
        var updatedAt = DateTimeOffset.UtcNow;
        var summary = new ChatThreadSummary(
            threadId.Trim(),
            ChatThreadTitles.FromMessages(trimmed),
            updatedAt,
            trimmed.Count);

        if (_db is null)
        {
            _memory[key] = trimmed;
            UpsertMemoryIndex(tenantId, audience, summary);
            return;
        }

        var indexKey = ChatThreadKeys.Index(tenantId, audience);
        var metaKey = MetaKey(tenantId, audience, threadId);
        var batch = _db.CreateBatch();
        var setTask = batch.StringSetAsync(key, payload, _ttl);
        var zaddTask = batch.SortedSetAddAsync(indexKey, threadId.Trim(), updatedAt.ToUnixTimeSeconds());
        var expireIndex = batch.KeyExpireAsync(indexKey, _ttl);
        var metaTask = batch.StringSetAsync(
            metaKey,
            JsonSerializer.Serialize(summary, Json),
            _ttl);
        batch.Execute();
        await Task.WhenAll(setTask, zaddTask, expireIndex, metaTask).ConfigureAwait(false);
    }

    public async Task<IReadOnlyList<ChatThreadSummary>> ListAsync(
        string tenantId,
        string audience,
        int take = 50,
        CancellationToken cancellationToken = default)
    {
        take = Math.Clamp(take, 1, 100);
        if (_db is null)
        {
            var indexKey = ChatThreadKeys.Index(tenantId, audience);
            if (!_memoryIndex.TryGetValue(indexKey, out var local))
                return [];
            return local
                .OrderByDescending(x => x.UpdatedAt)
                .Take(take)
                .ToList();
        }

        var ids = await _db.SortedSetRangeByRankAsync(
                ChatThreadKeys.Index(tenantId, audience),
                start: 0,
                stop: take - 1,
                Order.Descending)
            .ConfigureAwait(false);

        if (ids.Length == 0)
            return [];

        var results = new List<ChatThreadSummary>(ids.Length);
        foreach (var id in ids)
        {
            var threadId = (string?)id;
            if (string.IsNullOrWhiteSpace(threadId))
                continue;

            var meta = await _db.StringGetAsync(MetaKey(tenantId, audience, threadId)).ConfigureAwait(false);
            if (!meta.IsNullOrEmpty)
            {
                try
                {
                    var parsed = JsonSerializer.Deserialize<ChatThreadSummary>((string)meta!, Json);
                    if (parsed is not null)
                    {
                        results.Add(parsed);
                        continue;
                    }
                }
                catch (JsonException)
                {
                    // Fall through to message payload.
                }
            }

            var messages = await GetAsync(tenantId, audience, threadId, cancellationToken).ConfigureAwait(false);
            if (messages.Count == 0)
                continue;
            results.Add(new ChatThreadSummary(
                threadId,
                ChatThreadTitles.FromMessages(messages),
                DateTimeOffset.UtcNow,
                messages.Count));
        }

        return results;
    }

    private void UpsertMemoryIndex(string tenantId, string audience, ChatThreadSummary summary)
    {
        var indexKey = ChatThreadKeys.Index(tenantId, audience);
        if (!_memoryIndex.TryGetValue(indexKey, out var list))
        {
            list = [];
            _memoryIndex[indexKey] = list;
        }

        list.RemoveAll(x => string.Equals(x.ThreadId, summary.ThreadId, StringComparison.Ordinal));
        list.Insert(0, summary);
        if (list.Count > 100)
            list.RemoveRange(100, list.Count - 100);
    }

    private static string MetaKey(string tenantId, string audience, string threadId)
        => $"{ChatThreadKeys.Build(tenantId, audience, threadId)}:meta";

    public void Dispose() => _mux?.Dispose();
}

internal static class ChatThreadTitles
{
    public static string? FromMessages(IReadOnlyList<ChatThreadMessage> messages)
    {
        var firstUser = messages.FirstOrDefault(m =>
            string.Equals(m.Role, "user", StringComparison.OrdinalIgnoreCase)
            && !string.IsNullOrWhiteSpace(m.Text));
        if (firstUser is null)
            return null;
        var text = firstUser.Text.Trim().Replace('\n', ' ');
        return text.Length <= 60 ? text : text[..57] + "…";
    }
}
