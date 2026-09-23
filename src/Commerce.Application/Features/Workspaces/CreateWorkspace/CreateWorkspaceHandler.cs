using Finbuckle.MultiTenant.Abstractions;
using Microsoft.EntityFrameworkCore;
using Commerce.Application.Abstracts;
using Commerce.Application.Common.Abstracts;
using Commerce.Domain.Tenants;
using Commerce.Domain.Workspaces;

namespace Commerce.Application.Features.Workspaces.CreateWorkspace;

public sealed class CreateWorkspaceHandler(
    ICommerceDbContext db,
    IMultiTenantContextAccessor<CommerceTenantInfo> tenantAccessor)
    : ICommandHandler<CreateWorkspaceCommand, CreateWorkspaceResult>
{
    public async Task<CreateWorkspaceResult> Handle(CreateWorkspaceCommand request, CancellationToken cancellationToken)
    {
        var tenant = tenantAccessor.MultiTenantContext?.TenantInfo
            ?? throw new InvalidOperationException("Tenant is required.");

        var nameTaken = await db.Workspaces.AnyAsync(w => w.Name == request.Name, cancellationToken);
        if (nameTaken)
        {
            throw new InvalidOperationException($"Workspace '{request.Name}' already exists.");
        }

        if (request.IsDefault)
        {
            await ClearDefaultAsync(cancellationToken);
        }

        var workspace = new Workspace
        {
            TenantId = tenant.Id!,
            Name = request.Name.Trim(),
            EnvironmentKind = request.EnvironmentKind,
            IsDefault = request.IsDefault,
            IsSystem = false
        };

        db.Workspaces.Add(workspace);
        await db.SaveChangesAsync(cancellationToken);

        return new CreateWorkspaceResult(
            workspace.Id,
            workspace.Name,
            workspace.IsDefault,
            workspace.IsSystem,
            workspace.EnvironmentKind.ToString(),
            workspace.CreatedAt);
    }

    private async Task ClearDefaultAsync(CancellationToken cancellationToken)
    {
        var currentDefaults = await db.Workspaces.Where(w => w.IsDefault).ToListAsync(cancellationToken);
        foreach (var item in currentDefaults)
        {
            item.IsDefault = false;
        }
    }
}
