using Finbuckle.MultiTenant.Abstractions;
using Microsoft.EntityFrameworkCore;
using Commerce.Application.Abstracts;
using Commerce.Application.Common;
using Commerce.Application.Common.Abstracts;
using Commerce.Domain.Tenants;

namespace Commerce.Application.Features.Brains.GetBrainJob;

public sealed class GetBrainJobHandler(
    ICommerceDbContext db,
    IMultiTenantContextAccessor<CommerceTenantInfo> tenantAccessor,
    IWorkspaceContext workspaceContext)
    : IQueryHandler<GetBrainJobQuery, GetBrainJobResult?>
{
    public async Task<GetBrainJobResult?> Handle(GetBrainJobQuery request, CancellationToken cancellationToken)
    {
        _ = tenantAccessor.MultiTenantContext?.TenantInfo
            ?? throw new InvalidOperationException("Tenant is required.");
        var workspaceId = await WorkspaceRequirement.RequireWorkspaceIdAsync(
            workspaceContext, db, cancellationToken);

        var job = await db.BrainIngestJobs.AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == request.JobId && x.WorkspaceId == workspaceId, cancellationToken);
        return job is null ? null : BrainJobResults.Get(job);
    }
}
