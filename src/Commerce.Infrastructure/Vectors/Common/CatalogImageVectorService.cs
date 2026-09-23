using System.Collections.Concurrent;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Commerce.Application.Abstracts;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;

namespace Commerce.Infrastructure.Vectors.Common;

public sealed class CatalogImageVectorService : ICatalogImageVectorService
{
    private static readonly TimeSpan CacheTtl = TimeSpan.FromDays(14);
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    private readonly IImageVerbalizer _verbalizer;
    private readonly ITextEmbeddingService _embeddings;
    private readonly IDistributedCache _cache;
    private readonly ILogger<CatalogImageVectorService> _logger;
    private readonly ConcurrentDictionary<string, Task<CatalogImageVectorResult>> _inflight = new(StringComparer.Ordinal);

    public CatalogImageVectorService(
        IImageVerbalizer verbalizer,
        ITextEmbeddingService embeddings,
        IDistributedCache cache,
        ILogger<CatalogImageVectorService> logger)
    {
        _verbalizer = verbalizer;
        _embeddings = embeddings;
        _cache = cache;
        _logger = logger;
    }

    public async Task<CatalogImageVectorResult> ResolveAsync(
        string tenantId,
        string? imageUrl,
        CancellationToken cancellationToken = default)
    {
        var normalized = CatalogImageUrl.Normalize(imageUrl);
        if (normalized is null || string.IsNullOrWhiteSpace(tenantId))
            return CatalogImageVectorResult.Empty;

        var cacheKey = CacheKey(tenantId, normalized);
        var cached = await TryReadCacheAsync(cacheKey, cancellationToken).ConfigureAwait(false);
        if (cached is not null)
        {
            _logger.LogInformation(
                "Catalog image vector cache hit. TenantId={TenantId} Url={Url} VectorDims={Dims}",
                tenantId,
                Truncate(normalized, 120),
                cached.Vector?.Count ?? 0);
            return cached;
        }

        _logger.LogInformation(
            "Catalog image vector cache miss. TenantId={TenantId} Url={Url}",
            tenantId,
            Truncate(normalized, 120));

        var task = _inflight.GetOrAdd(
            cacheKey,
            _ => ComputeAsync(tenantId, normalized, cacheKey));

        try
        {
            return await task.WaitAsync(cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            _inflight.TryRemove(new KeyValuePair<string, Task<CatalogImageVectorResult>>(cacheKey, task));
        }
    }

    private async Task<CatalogImageVectorResult> ComputeAsync(
        string tenantId,
        string normalizedUrl,
        string cacheKey)
    {
        // Shared compute must not bind to a single caller's cancellation token.
        var cancellationToken = CancellationToken.None;

        // Another waiter may have populated Redis while we waited for single-flight.
        var cached = await TryReadCacheAsync(cacheKey, cancellationToken).ConfigureAwait(false);
        if (cached is not null)
            return cached;

        _logger.LogInformation(
            "Catalog image vector compute. TenantId={TenantId} Url={Url}",
            tenantId,
            Truncate(normalizedUrl, 200));

        var caption = await _verbalizer.VerbalizeUrlAsync(normalizedUrl, cancellationToken).ConfigureAwait(false);
        if (string.IsNullOrWhiteSpace(caption))
        {
            _logger.LogWarning(
                "Catalog image vector caption failed. TenantId={TenantId} Url={Url}",
                tenantId,
                Truncate(normalizedUrl, 200));
            return CatalogImageVectorResult.Empty;
        }

        var vector = await _embeddings.EmbedAsync(caption, cancellationToken).ConfigureAwait(false);
        if (vector is not { Length: > 0 })
            return CatalogImageVectorResult.Empty;

        var result = new CatalogImageVectorResult(caption, vector);
        await TryWriteCacheAsync(cacheKey, result, cancellationToken).ConfigureAwait(false);

        _logger.LogInformation(
            "Catalog image vector computed. TenantId={TenantId} Url={Url} VectorDims={Dims} Caption={Caption}",
            tenantId,
            Truncate(normalizedUrl, 200),
            vector.Length,
            Truncate(caption, 120));

        return result;
    }

    private async Task<CatalogImageVectorResult?> TryReadCacheAsync(string cacheKey, CancellationToken cancellationToken)
    {
        try
        {
            var bytes = await _cache.GetAsync(cacheKey, cancellationToken).ConfigureAwait(false);
            if (bytes is null || bytes.Length == 0)
                return null;

            var entry = JsonSerializer.Deserialize<CacheEntry>(bytes, Json);
            if (entry?.Vector is not { Length: > 0 })
                return null;

            return new CatalogImageVectorResult(entry.Caption, entry.Vector);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogDebug(ex, "Catalog image vector cache read failed. Key={Key}", cacheKey);
            return null;
        }
    }

    private async Task TryWriteCacheAsync(
        string cacheKey,
        CatalogImageVectorResult result,
        CancellationToken cancellationToken)
    {
        if (!result.HasVector)
            return;

        try
        {
            var entry = new CacheEntry(result.Caption, result.Vector!.ToArray());
            var bytes = JsonSerializer.SerializeToUtf8Bytes(entry, Json);
            await _cache.SetAsync(
                cacheKey,
                bytes,
                new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = CacheTtl },
                cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogDebug(ex, "Catalog image vector cache write failed. Key={Key}", cacheKey);
        }
    }

    internal static string? NormalizeUrl(string? imageUrl)
        => CatalogImageUrl.Normalize(imageUrl);

    private static string CacheKey(string tenantId, string normalizedUrl)
    {
        var hash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(normalizedUrl))).ToLowerInvariant();
        return $"catalog-img-vec:{tenantId.Trim()}:{hash}";
    }

    private static string Truncate(string? value, int max = 160)
    {
        if (string.IsNullOrEmpty(value))
            return string.Empty;
        return value.Length <= max ? value : value[..max] + "…";
    }

    private sealed record CacheEntry(string? Caption, float[] Vector);
}
