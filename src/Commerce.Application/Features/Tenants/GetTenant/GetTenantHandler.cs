using Commerce.Application.Abstracts;
using Commerce.Application.Common.Abstracts;

namespace Commerce.Application.Features.Tenants.GetTenant;

public sealed class GetTenantHandler(ITenantStore tenants)
    : IQueryHandler<GetTenantQuery, GetTenantResult?>
{
    public async Task<GetTenantResult?> Handle(GetTenantQuery request, CancellationToken cancellationToken)
    {
        var tenant = await tenants.GetByIdentifierAsync(request.Identifier, cancellationToken);
        return tenant is null ? null : new GetTenantResult(tenant.Id, tenant.Identifier, tenant.Name);
    }
}
