using Finbuckle.MultiTenant.Abstractions;
using Commerce.Application.Abstracts;
using Commerce.Application.Common.Abstracts;
using Commerce.Domain.Tenants;

namespace Commerce.Application.Features.Agents.SaveAgentDefinition;

public sealed class SaveAgentDefinitionHandler(
    IAgentDefinitionStore store,
    IMultiTenantContextAccessor<CommerceTenantInfo> tenantAccessor)
    : ICommandHandler<SaveAgentDefinitionCommand, SaveAgentDefinitionResult>
{
    public async Task<SaveAgentDefinitionResult> Handle(SaveAgentDefinitionCommand request, CancellationToken cancellationToken)
    {
        var tenant = tenantAccessor.MultiTenantContext?.TenantInfo
            ?? throw new InvalidOperationException("Tenant is required.");

        var saved = await store.SaveTenantYamlAsync(tenant.Id!, request.AgentKey, request.Yaml, cancellationToken);
        return new SaveAgentDefinitionResult(saved.Key, saved.Hash, saved.IsTenantOverride);
    }
}
