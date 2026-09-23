using Commerce.Application.Abstracts;
using Commerce.Application.Common.Abstracts;

namespace Commerce.Application.Features.Tenants.UpdateTenant;

public sealed class UpdateTenantHandler(ITenantStore tenants)
    : ICommandHandler<UpdateTenantCommand, UpdateTenantResult>
{
    public async Task<UpdateTenantResult> Handle(UpdateTenantCommand request, CancellationToken cancellationToken)
    {
        var tenant = await tenants.GetByIdentifierAsync(request.Identifier, cancellationToken)
            ?? throw new KeyNotFoundException($"Tenant '{request.Identifier}' was not found.");
        tenant.Name = request.Name;
        await tenants.UpdateAsync(tenant, cancellationToken);
        return new UpdateTenantResult(tenant.Id, tenant.Identifier, tenant.Name);
    }
}
