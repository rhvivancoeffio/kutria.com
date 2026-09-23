using Microsoft.Extensions.Caching.Distributed;
using Commerce.Application.Features.CreateProduct;

namespace Commerce.Infrastructure.Agents.Definitions.ProductOnboardingCoordinator;

public sealed class RedisOnboardingImageStore(IDistributedCache cache) : IOnboardingImageStore
{
    private static readonly TimeSpan Ttl = TimeSpan.FromHours(1);

    public async Task SaveAsync(
        string tenantId,
        string workflowId,
        byte[] bytes,
        string contentType,
        CancellationToken cancellationToken = default)
    {
        var meta = System.Text.Encoding.UTF8.GetBytes(contentType);
        await cache.SetAsync(
            MetaKey(tenantId, workflowId),
            meta,
            new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = Ttl },
            cancellationToken);
        await cache.SetAsync(
            DataKey(tenantId, workflowId),
            bytes,
            new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = Ttl },
            cancellationToken);
    }

    public async Task<(byte[] Bytes, string ContentType)?> GetAsync(
        string tenantId,
        string workflowId,
        CancellationToken cancellationToken = default)
    {
        var meta = await cache.GetAsync(MetaKey(tenantId, workflowId), cancellationToken);
        var data = await cache.GetAsync(DataKey(tenantId, workflowId), cancellationToken);
        if (meta is null || data is null || data.Length == 0)
            return null;

        var contentType = System.Text.Encoding.UTF8.GetString(meta);
        if (string.IsNullOrWhiteSpace(contentType))
            contentType = "image/jpeg";
        return (data, contentType);
    }

    private static string MetaKey(string tenantId, string workflowId)
        => $"onboarding-image-meta:{tenantId}:{workflowId}";

    private static string DataKey(string tenantId, string workflowId)
        => $"onboarding-image:{tenantId}:{workflowId}";
}
