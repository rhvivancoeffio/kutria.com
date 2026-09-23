using System.Text.Json;
using Commerce.Application.Features.CreateProduct;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;

namespace Commerce.Infrastructure.Agents.Definitions.ProductOnboardingCoordinator;

public sealed class RedisOnboardingWorkflowStore(
    IDistributedCache cache,
    ILogger<RedisOnboardingWorkflowStore> logger) : IOnboardingWorkflowStore
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);
    private static readonly TimeSpan Ttl = TimeSpan.FromHours(1);

    public async Task SaveAsync(OnboardingWorkflowState state, CancellationToken cancellationToken = default)
    {
        var key = Key(state.TenantId, state.WorkflowId);
        var bytes = JsonSerializer.SerializeToUtf8Bytes(state, Json);
        await cache.SetAsync(
            key,
            bytes,
            new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = Ttl },
            cancellationToken);
        logger.LogInformation(
            "Onboarding workflow saved. TenantId={TenantId} WorkflowId={WorkflowId} Status={Status}",
            state.TenantId,
            state.WorkflowId,
            state.Status);
    }

    public async Task<OnboardingWorkflowState?> GetAsync(
        string tenantId,
        string workflowId,
        CancellationToken cancellationToken = default)
    {
        var bytes = await cache.GetAsync(Key(tenantId, workflowId), cancellationToken);
        if (bytes is null || bytes.Length == 0)
            return null;

        return JsonSerializer.Deserialize<OnboardingWorkflowState>(bytes, Json);
    }

    public async Task DeleteAsync(string tenantId, string workflowId, CancellationToken cancellationToken = default)
    {
        await cache.RemoveAsync(Key(tenantId, workflowId), cancellationToken);
    }

    private static string Key(string tenantId, string workflowId)
        => $"onboarding:{tenantId}:{workflowId}";
}
