using Finbuckle.MultiTenant.Abstractions;
using Microsoft.EntityFrameworkCore;
using Commerce.Application.Abstracts;
using Commerce.Application.Common;
using Commerce.Application.Common.Abstracts;
using Commerce.Domain.Tenants;

namespace Commerce.Application.Features.ApiKeys.RevokeApiKey;

public sealed class RevokeApiKeyHandler(
    ICommerceDbContext db,
    IMultiTenantContextAccessor<CommerceTenantInfo> tenantAccessor,
    IWorkspaceContext workspaceContext)
    : ICommandHandler<RevokeApiKeyCommand, RevokeApiKeyResult>
{
    public async Task<RevokeApiKeyResult> Handle(RevokeApiKeyCommand request, CancellationToken cancellationToken)
    {
        _ = tenantAccessor.MultiTenantContext?.TenantInfo
            ?? throw new InvalidOperationException("Tenant is required.");
        var workspaceId = await WorkspaceRequirement.RequireWorkspaceIdAsync(
            workspaceContext, db, cancellationToken);

        var entity = await db.TenantApiKeys
                .FirstOrDefaultAsync(x => x.Id == request.Id && x.WorkspaceId == workspaceId, cancellationToken)
            ?? throw new KeyNotFoundException("API key not found.");

        entity.IsActive = false;
        entity.RevokedAt = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync(cancellationToken);
        return new RevokeApiKeyResult(entity.Id, false);
    }
}
