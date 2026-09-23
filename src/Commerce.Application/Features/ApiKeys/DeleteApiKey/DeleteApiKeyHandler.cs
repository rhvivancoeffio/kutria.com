using Finbuckle.MultiTenant.Abstractions;
using Microsoft.EntityFrameworkCore;
using Commerce.Application.Abstracts;
using Commerce.Application.Common;
using Commerce.Application.Common.Abstracts;
using Commerce.Domain.Tenants;

namespace Commerce.Application.Features.ApiKeys.DeleteApiKey;

public sealed class DeleteApiKeyHandler(
    ICommerceDbContext db,
    IMultiTenantContextAccessor<CommerceTenantInfo> tenantAccessor,
    IWorkspaceContext workspaceContext)
    : ICommandHandler<DeleteApiKeyCommand, DeleteApiKeyResult>
{
    public async Task<DeleteApiKeyResult> Handle(DeleteApiKeyCommand request, CancellationToken cancellationToken)
    {
        _ = tenantAccessor.MultiTenantContext?.TenantInfo
            ?? throw new InvalidOperationException("Tenant is required.");
        var workspaceId = await WorkspaceRequirement.RequireWorkspaceIdAsync(
            workspaceContext, db, cancellationToken);

        var entity = await db.TenantApiKeys
                .FirstOrDefaultAsync(x => x.Id == request.Id && x.WorkspaceId == workspaceId, cancellationToken)
            ?? throw new KeyNotFoundException("API key not found.");

        db.TenantApiKeys.Remove(entity);
        await db.SaveChangesAsync(cancellationToken);
        return new DeleteApiKeyResult(request.Id);
    }
}
