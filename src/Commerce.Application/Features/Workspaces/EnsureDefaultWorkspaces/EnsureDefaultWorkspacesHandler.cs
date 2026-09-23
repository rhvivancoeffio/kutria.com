using Finbuckle.MultiTenant.Abstractions;
using Microsoft.EntityFrameworkCore;
using Commerce.Application.Abstracts;
using Commerce.Application.Common.Abstracts;
using Commerce.Domain.Tenants;
using Commerce.Domain.Workspaces;

namespace Commerce.Application.Features.Workspaces.EnsureDefaultWorkspaces;

public sealed class EnsureDefaultWorkspacesHandler(
    ICommerceDbContext db,
    IMultiTenantContextAccessor<CommerceTenantInfo> tenantAccessor)
    : ICommandHandler<EnsureDefaultWorkspacesCommand, EnsureDefaultWorkspacesResult>
{
    public async Task<EnsureDefaultWorkspacesResult> Handle(
        EnsureDefaultWorkspacesCommand request,
        CancellationToken cancellationToken)
    {
        var tenant = tenantAccessor.MultiTenantContext?.TenantInfo
            ?? throw new InvalidOperationException("Tenant is required.");

        var existing = await db.Workspaces
            .Where(w => w.IsSystem)
            .ToListAsync(cancellationToken);

        var sandbox = existing.FirstOrDefault(w => w.EnvironmentKind == WorkspaceEnvironment.Sandbox);
        var production = existing.FirstOrDefault(w => w.EnvironmentKind == WorkspaceEnvironment.Production);
        var created = false;

        if (sandbox is null)
        {
            sandbox = new Workspace
            {
                TenantId = tenant.Id!,
                Name = "Sandbox",
                EnvironmentKind = WorkspaceEnvironment.Sandbox,
                IsDefault = true,
                IsSystem = true
            };
            db.Workspaces.Add(sandbox);
            created = true;
        }

        if (production is null)
        {
            production = new Workspace
            {
                TenantId = tenant.Id!,
                Name = "Production",
                EnvironmentKind = WorkspaceEnvironment.Production,
                IsDefault = false,
                IsSystem = true
            };
            db.Workspaces.Add(production);
            created = true;
        }

        if (created)
        {
            await db.SaveChangesAsync(cancellationToken);
        }

        return new EnsureDefaultWorkspacesResult(sandbox.Id, production.Id, created);
    }
}
