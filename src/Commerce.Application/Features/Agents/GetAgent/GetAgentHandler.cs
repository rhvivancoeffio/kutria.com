using Finbuckle.MultiTenant.Abstractions;
using Commerce.Application.Abstracts;
using Commerce.Application.Common.Abstracts;
using Commerce.Domain.Tenants;

namespace Commerce.Application.Features.Agents.GetAgent;

public sealed class GetAgentHandler(
    IAgentDefinitionStore store,
    IMultiTenantContextAccessor<CommerceTenantInfo> tenantAccessor)
    : IQueryHandler<GetAgentQuery, GetAgentResult?>
{
    public async Task<GetAgentResult?> Handle(GetAgentQuery request, CancellationToken cancellationToken)
    {
        var tenant = tenantAccessor.MultiTenantContext?.TenantInfo
            ?? throw new InvalidOperationException("Tenant is required.");

        var agent = await store.GetAsync(tenant.Id!, request.AgentKey, cancellationToken);
        return agent is null ? null : new GetAgentResult(agent.Key, agent.Yaml, agent.IsTenantOverride, agent.Hash);
    }
}
