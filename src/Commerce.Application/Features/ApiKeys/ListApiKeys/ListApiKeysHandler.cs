using Finbuckle.MultiTenant.Abstractions;
using Microsoft.EntityFrameworkCore;
using Commerce.Application.Abstracts;
using Commerce.Application.Common;
using Commerce.Application.Common.Abstracts;
using Commerce.Domain.Tenants;

namespace Commerce.Application.Features.ApiKeys.ListApiKeys;

public sealed class ListApiKeysHandler(
    ICommerceDbContext db,
    IMultiTenantContextAccessor<CommerceTenantInfo> tenantAccessor,
    IWorkspaceContext workspaceContext)
    : IQueryHandler<ListApiKeysQuery, ListApiKeysResult>
{
    public async Task<ListApiKeysResult> Handle(ListApiKeysQuery request, CancellationToken cancellationToken)
    {
        _ = tenantAccessor.MultiTenantContext?.TenantInfo
            ?? throw new InvalidOperationException("Tenant is required.");
        var workspaceId = await WorkspaceRequirement.RequireWorkspaceIdAsync(
            workspaceContext, db, cancellationToken);

        var items = await db.TenantApiKeys
            .AsNoTracking()
            .Where(x => x.WorkspaceId == workspaceId)
            .OrderByDescending(x => x.CreatedAt)
            .Select(x => new ApiKeyListItem(
                x.Id,
                x.Name,
                x.KeyPrefix,
                x.CreatedAt,
                x.LastUsedAt,
                x.IsActive && x.RevokedAt == null))
            .ToListAsync(cancellationToken);

        return new ListApiKeysResult(items);
    }
}
