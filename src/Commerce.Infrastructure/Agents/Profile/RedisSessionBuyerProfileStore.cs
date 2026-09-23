using System.Text.Json;
using Commerce.Application.Abstracts;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;

namespace Commerce.Infrastructure.Agents.Profile;

public sealed class RedisSessionBuyerProfileStore : ISessionBuyerProfileStore, IDisposable
{
    public static readonly TimeSpan DefaultTtl = TimeSpan.FromHours(24);

    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    private readonly IConnectionMultiplexer? _mux;
    private readonly IDatabase? _db;
    private readonly ILogger<RedisSessionBuyerProfileStore> _logger;
    private readonly TimeSpan _ttl;
    private readonly Dictionary<string, SessionBuyerProfile> _memory = new(StringComparer.Ordinal);

    public RedisSessionBuyerProfileStore(
        IConfiguration configuration,
        ILogger<RedisSessionBuyerProfileStore> logger)
    {
        _logger = logger;
        _ttl = TimeSpan.TryParse(configuration["Chat:HistoryTtl"], out var ttl) && ttl > TimeSpan.Zero
            ? ttl
            : DefaultTtl;

        var redis = configuration.GetConnectionString("Redis");
        if (string.IsNullOrWhiteSpace(redis))
        {
            _logger.LogWarning("Buyer profile using in-process memory. Redis connection is missing.");
            return;
        }

        try
        {
            _mux = ConnectionMultiplexer.Connect(redis);
            _db = _mux.GetDatabase();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Buyer profile falling back to in-process memory. Redis connect failed.");
        }
    }

    public async Task<SessionBuyerProfile> GetAsync(
        string tenantId,
        string threadId,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(tenantId) || string.IsNullOrWhiteSpace(threadId))
            return SessionBuyerProfile.Empty(tenantId ?? string.Empty, threadId ?? string.Empty);

        var key = SessionBuyerProfileKeys.Build(tenantId, threadId);
        if (_db is null)
        {
            return _memory.TryGetValue(key, out var local)
                ? local
                : SessionBuyerProfile.Empty(tenantId, threadId);
        }

        var value = await _db.StringGetAsync(key).ConfigureAwait(false);
        if (value.IsNullOrEmpty)
            return SessionBuyerProfile.Empty(tenantId, threadId);

        try
        {
            return JsonSerializer.Deserialize<SessionBuyerProfile>((string)value!, Json)
                   ?? SessionBuyerProfile.Empty(tenantId, threadId);
        }
        catch (JsonException ex)
        {
            _logger.LogWarning(ex, "Buyer profile ignored malformed payload for {Key}", key);
            return SessionBuyerProfile.Empty(tenantId, threadId);
        }
    }

    public async Task SaveAsync(
        SessionBuyerProfile profile,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(profile.TenantId) || string.IsNullOrWhiteSpace(profile.ThreadId))
            throw new ArgumentException("TenantId and ThreadId are required.");

        var key = SessionBuyerProfileKeys.Build(profile.TenantId, profile.ThreadId);
        var toSave = profile with { UpdatedAt = DateTimeOffset.UtcNow };
        var payload = JsonSerializer.Serialize(toSave, Json);

        if (_db is null)
        {
            _memory[key] = toSave;
            return;
        }

        await _db.StringSetAsync(key, payload, _ttl).ConfigureAwait(false);
    }

    public void Dispose() => _mux?.Dispose();
}
