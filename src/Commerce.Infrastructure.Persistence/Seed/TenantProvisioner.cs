using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Commerce.Application.Abstracts;
using Commerce.Domain.Billing;
using Commerce.Domain.Tenants;
using Commerce.Domain.Workspaces;

namespace Commerce.Infrastructure.Persistence.Seed;

public sealed class TenantProvisioner(IServiceScopeFactory scopeFactory) : ITenantProvisioner
{
    public Task EnsureDefaultsAsync(CommerceTenantInfo tenant, CancellationToken cancellationToken = default)
        => EnsureDefaultsAsync(tenant, "free", cancellationToken);

    public async Task EnsureDefaultsAsync(CommerceTenantInfo tenant, string planCode, CancellationToken cancellationToken = default)
    {
        using var scope = scopeFactory.CreateScope();
        TenantBootstrap.SetCurrentTenant(scope.ServiceProvider, tenant);
        var db = scope.ServiceProvider.GetRequiredService<ICommerceDbContext>();

        await EnsureWorkspacesAsync(db, tenant.Id!, cancellationToken);
        await EnsureBillingAsync(db, tenant.Id!, planCode, cancellationToken);
        await db.SaveChangesAsync(cancellationToken);
    }

    private static async Task EnsureWorkspacesAsync(
        ICommerceDbContext db,
        string tenantId,
        CancellationToken cancellationToken)
    {
        var system = await db.Workspaces.Where(w => w.IsSystem).ToListAsync(cancellationToken);

        if (system.All(w => w.EnvironmentKind != WorkspaceEnvironment.Sandbox))
        {
            db.Workspaces.Add(new Workspace
            {
                TenantId = tenantId,
                Name = "Sandbox",
                EnvironmentKind = WorkspaceEnvironment.Sandbox,
                IsDefault = true,
                IsSystem = true
            });
        }

        if (system.All(w => w.EnvironmentKind != WorkspaceEnvironment.Production))
        {
            db.Workspaces.Add(new Workspace
            {
                TenantId = tenantId,
                Name = "Production",
                EnvironmentKind = WorkspaceEnvironment.Production,
                IsDefault = false,
                IsSystem = true
            });
        }
    }

    private static async Task EnsureBillingAsync(
        ICommerceDbContext db,
        string tenantId,
        string planCode,
        CancellationToken cancellationToken)
    {
        var exists = await db.TenantBillings.AnyAsync(cancellationToken);
        if (exists)
        {
            return;
        }

        var plan = string.IsNullOrWhiteSpace(planCode) ? "free" : planCode.Trim().ToLowerInvariant();
        db.TenantBillings.Add(new TenantBilling
        {
            TenantId = tenantId,
            PlanCode = plan,
            Status = BillingStatus.Trialing,
            TrialEndsAt = DateTimeOffset.UtcNow.AddDays(14),
            UpdatedAt = DateTimeOffset.UtcNow
        });
    }
}
