using Commerce.Application.Abstracts;
using Commerce.Application.Common.Abstracts;

namespace Commerce.Application.Features.Tenants.ListTenants;

public sealed class ListTenantsHandler(ITenantStore tenants)
    : IQueryHandler<ListTenantsQuery, ListTenantsResult>
{
    public async Task<ListTenantsResult> Handle(ListTenantsQuery request, CancellationToken cancellationToken)
    {
        var items = await tenants.ListAsync(cancellationToken);
        return new ListTenantsResult(items.Select(t => new TenantListItem(t.Id, t.Identifier, t.Name)).ToList());
    }
}
