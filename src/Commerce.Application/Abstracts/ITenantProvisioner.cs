using Commerce.Domain.Tenants;

namespace Commerce.Application.Abstracts;

public interface ITenantProvisioner
{
    Task EnsureDefaultsAsync(CommerceTenantInfo tenant, CancellationToken cancellationToken = default);
    Task EnsureDefaultsAsync(CommerceTenantInfo tenant, string planCode, CancellationToken cancellationToken = default);
}
