using Commerce.Application.Abstracts;

namespace Commerce.Infrastructure.Vectors.Common;

public sealed class InMemoryVectorStore : IVectorStore
{
    private readonly List<StoredPoint> _points = [];
    private readonly object _gate = new();

    public Task UpsertAsync(VectorPoint point, CancellationToken cancellationToken = default)
    {
        RequireTenant(point.TenantId);
        lock (_gate)
        {
            _points.RemoveAll(x => x.Id == point.Id && x.TenantId == point.TenantId);
            _points.Add(new StoredPoint(point.Id, point.TenantId, point.Text, point.Payload));
        }

        return Task.CompletedTask;
    }

    public Task<IReadOnlyList<VectorHit>> SearchAsync(
        string tenantId,
        ReadOnlyMemory<float> vector,
        int limit,
        CancellationToken cancellationToken = default)
    {
        RequireTenant(tenantId);
        lock (_gate)
        {
            var hits = _points
                .Where(x => x.TenantId == tenantId)
                .Select(x => new VectorHit(x.Id, 1f, x.Payload))
                .Take(Math.Max(limit, 0))
                .ToList();
            return Task.FromResult<IReadOnlyList<VectorHit>>(hits);
        }
    }

    private static void RequireTenant(string tenantId)
    {
        if (string.IsNullOrWhiteSpace(tenantId))
        {
            throw new ArgumentException("tenantId is required.", nameof(tenantId));
        }
    }

    private sealed record StoredPoint(string Id, string TenantId, string Text, IReadOnlyDictionary<string, string> Payload);
}
