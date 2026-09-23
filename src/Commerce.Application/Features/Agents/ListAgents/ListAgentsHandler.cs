using Finbuckle.MultiTenant.Abstractions;
using Commerce.Application.Abstracts;
using Commerce.Application.Common.Abstracts;
using Commerce.Domain.Tenants;

namespace Commerce.Application.Features.Agents.ListAgents;

public sealed class ListAgentsHandler(
    IAgentDefinitionStore store,
    IMultiTenantContextAccessor<CommerceTenantInfo> tenantAccessor)
    : IQueryHandler<ListAgentsQuery, ListAgentsResult>
{
    public async Task<ListAgentsResult> Handle(ListAgentsQuery request, CancellationToken cancellationToken)
    {
        var tenant = tenantAccessor.MultiTenantContext?.TenantInfo
            ?? throw new InvalidOperationException("Tenant is required.");

        var agents = await store.ListAsync(tenant.Id!, cancellationToken);
        var items = agents.Select(a => new AgentListItem(a.Key, a.Kind, a.Name, a.Model, a.Queue, a.IsTenantOverride, a.Tools)).ToList();
        return new ListAgentsResult(items);
    }
}
