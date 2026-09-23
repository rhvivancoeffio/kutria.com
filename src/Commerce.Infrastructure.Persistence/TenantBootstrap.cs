using Finbuckle.MultiTenant.Abstractions;
using Microsoft.Extensions.DependencyInjection;
using Commerce.Domain.Tenants;

namespace Commerce.Infrastructure.Persistence;

public static class TenantBootstrap
{
    public static void SetCurrentTenant(IServiceProvider services, CommerceTenantInfo tenant)
    {
        var context = new MultiTenantContext<CommerceTenantInfo>(tenant);
        var setter = services.GetRequiredService<IMultiTenantContextSetter>();
        setter.MultiTenantContext = context;
    }
}
