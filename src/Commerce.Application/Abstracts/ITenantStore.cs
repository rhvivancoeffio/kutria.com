using Commerce.Domain.Tenants;

namespace Commerce.Application.Abstracts;

public interface ITenantStore
{
    Task<IReadOnlyList<CommerceTenantInfo>> ListAsync(CancellationToken cancellationToken = default);
    Task<CommerceTenantInfo?> GetByIdAsync(string id, CancellationToken cancellationToken = default);
    Task<CommerceTenantInfo?> GetByIdentifierAsync(string identifier, CancellationToken cancellationToken = default);
    Task<CommerceTenantInfo> AddAsync(CommerceTenantInfo tenant, CancellationToken cancellationToken = default);
    Task UpdateAsync(CommerceTenantInfo tenant, CancellationToken cancellationToken = default);
}
