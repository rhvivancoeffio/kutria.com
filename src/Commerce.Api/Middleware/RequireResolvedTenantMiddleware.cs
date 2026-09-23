using Finbuckle.MultiTenant.Abstractions;
using Commerce.Domain.Tenants;

namespace Commerce.Api.Middleware;

/// <summary>
/// After Finbuckle resolves the tenant, block /t/{identifier}/... when the tenant is missing.
/// Avoids MultiTenantDbContext query-filter NREs when TenantInfo is null.
/// </summary>
public sealed class RequireResolvedTenantMiddleware(RequestDelegate next)
{
    public async Task InvokeAsync(
        HttpContext context,
        IMultiTenantContextAccessor<CommerceTenantInfo> tenantAccessor)
    {
        if (RequiresTenant(context.Request.Path)
            && !IsTenantResolved(tenantAccessor.MultiTenantContext))
        {
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            await context.Response.WriteAsJsonAsync(new { error = "Tenant not found." });
            return;
        }

        await next(context);
    }

    private static bool RequiresTenant(PathString path)
    {
        var value = path.Value;
        return !string.IsNullOrEmpty(value)
            && value.StartsWith("/t/", StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsTenantResolved(IMultiTenantContext<CommerceTenantInfo>? multiTenantContext)
        => multiTenantContext is { IsResolved: true, TenantInfo: not null };
}
