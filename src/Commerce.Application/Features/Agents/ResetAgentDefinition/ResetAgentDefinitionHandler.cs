using Finbuckle.MultiTenant.Abstractions;
using Commerce.Application.Abstracts;
using Commerce.Application.Common.Abstracts;
using Commerce.Domain.Tenants;

namespace Commerce.Application.Features.Agents.ResetAgentDefinition;

public sealed class ResetAgentDefinitionHandler(
    IAgentDefinitionStore store,
    IMultiTenantContextAccessor<CommerceTenantInfo> tenantAccessor)
    : ICommandHandler<ResetAgentDefinitionCommand, ResetAgentDefinitionResult>
{
    public async Task<ResetAgentDefinitionResult> Handle(ResetAgentDefinitionCommand request, CancellationToken cancellationToken)
    {
        var tenant = tenantAccessor.MultiTenantContext?.TenantInfo
            ?? throw new InvalidOperationException("Tenant is required.");

        await store.ResetTenantAsync(tenant.Id!, request.AgentKey, cancellationToken);
        return new ResetAgentDefinitionResult(request.AgentKey, true);
    }
}
