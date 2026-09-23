using Commerce.Application.Abstracts;
using Commerce.Application.Common.Abstracts;
using Commerce.Domain.Tenants;

namespace Commerce.Application.Features.Tenants.CreateTenant;

public sealed class CreateTenantHandler(ITenantStore tenants, ITenantProvisioner provisioner)
    : ICommandHandler<CreateTenantCommand, CreateTenantResult>
{
    public async Task<CreateTenantResult> Handle(CreateTenantCommand request, CancellationToken cancellationToken)
    {
        var existing = await tenants.GetByIdentifierAsync(request.Identifier, cancellationToken);
        if (existing is not null)
        {
            throw new InvalidOperationException($"Tenant '{request.Identifier}' already exists.");
        }

        var tenant = await tenants.AddAsync(new CommerceTenantInfo
        {
            Id = request.Identifier,
            Identifier = request.Identifier,
            Name = request.Name
        }, cancellationToken);

        await provisioner.EnsureDefaultsAsync(tenant, cancellationToken);

        return new CreateTenantResult(tenant.Id, tenant.Identifier, tenant.Name);
    }
}
