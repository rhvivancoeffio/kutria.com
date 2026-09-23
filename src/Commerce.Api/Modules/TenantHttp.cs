using Commerce.Domain.Tenants;
using Commerce.Infrastructure.Persistence;

namespace Commerce.Api.Modules;

internal static class TenantHttp
{
    public static async Task<bool> SetBySlugAsync(IServiceProvider services, string slug, CancellationToken cancellationToken)
    {
        var store = services.GetRequiredService<Commerce.Application.Abstracts.ITenantStore>();
        var tenant = await store.GetByIdentifierAsync(slug, cancellationToken);
        if (tenant is null)
        {
            return false;
        }

        TenantBootstrap.SetCurrentTenant(services, tenant);
        return true;
    }
}
