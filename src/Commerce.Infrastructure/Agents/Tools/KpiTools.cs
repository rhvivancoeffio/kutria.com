using Commerce.Application.Abstracts;

namespace Commerce.Infrastructure.Agents.Tools;

public sealed class KpiTools : IKpiTools
{
    public Task<string> GetKpiAsync(string name, CancellationToken cancellationToken = default)
        => NotImplemented(nameof(GetKpiAsync));

    public Task<string> GetOrdersDelayedAsync(CancellationToken cancellationToken = default)
        => NotImplemented(nameof(GetOrdersDelayedAsync));

    public Task<string> GetClaimsBreakdownAsync(CancellationToken cancellationToken = default)
        => NotImplemented(nameof(GetClaimsBreakdownAsync));

    public Task<string> GetMarketplaceHealthAsync(CancellationToken cancellationToken = default)
        => NotImplemented(nameof(GetMarketplaceHealthAsync));

    private static Task<string> NotImplemented(string name)
        => Task.FromResult($"{name} is not implemented. KPI stores are not in this slice.");
}
