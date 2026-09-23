using Microsoft.EntityFrameworkCore;
using Commerce.Application.Abstracts;
using Commerce.Domain.Tenants;

namespace Commerce.Infrastructure.Persistence;

public sealed class EfTenantStore(ICommerceTenantDatabase db) : ITenantStore
{
    public async Task<IReadOnlyList<CommerceTenantInfo>> ListAsync(CancellationToken cancellationToken = default)
        => await db.TenantInfo.OrderBy(t => t.Identifier).ToListAsync(cancellationToken);

    public Task<CommerceTenantInfo?> GetByIdAsync(string id, CancellationToken cancellationToken = default)
        => db.TenantInfo.FirstOrDefaultAsync(t => t.Id == id, cancellationToken);

    public Task<CommerceTenantInfo?> GetByIdentifierAsync(string identifier, CancellationToken cancellationToken = default)
        => db.TenantInfo.FirstOrDefaultAsync(t => t.Identifier == identifier, cancellationToken);

    public async Task<CommerceTenantInfo> AddAsync(CommerceTenantInfo tenant, CancellationToken cancellationToken = default)
    {
        db.TenantInfo.Add(tenant);
        await db.SaveChangesAsync(cancellationToken);
        return tenant;
    }

    public async Task UpdateAsync(CommerceTenantInfo tenant, CancellationToken cancellationToken = default)
    {
        db.TenantInfo.Update(tenant);
        await db.SaveChangesAsync(cancellationToken);
    }
}
