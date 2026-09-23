using Finbuckle.MultiTenant.Abstractions;
using Microsoft.EntityFrameworkCore;
using Commerce.Application.Abstracts;
using Commerce.Application.Common;
using Commerce.Application.Common.Abstracts;
using Commerce.Domain.Brains;
using Commerce.Domain.Tenants;

namespace Commerce.Application.Features.Policies.GetPolicyEvalStatus;

public sealed class GetPolicyEvalStatusHandler(
    ICommerceDbContext db,
    IMultiTenantContextAccessor<CommerceTenantInfo> tenantAccessor,
    IWorkspaceContext workspaceContext)
    : IQueryHandler<GetPolicyEvalStatusQuery, GetPolicyEvalStatusResult?>
{
    public async Task<GetPolicyEvalStatusResult?> Handle(GetPolicyEvalStatusQuery request, CancellationToken cancellationToken)
    {
        _ = tenantAccessor.MultiTenantContext?.TenantInfo
            ?? throw new InvalidOperationException("Tenant is required.");
        var workspaceId = await WorkspaceRequirement.RequireWorkspaceIdAsync(
            workspaceContext, db, cancellationToken);
        var job = await db.BrainIngestJobs.AsNoTracking()
            .FirstOrDefaultAsync(
                x => x.Id == request.JobId && x.WorkspaceId == workspaceId && x.BrainKey == BrainKeys.Policy,
                cancellationToken);
        if (job is null)
        {
            return null;
        }

        var runId = await db.PolicyEvalRuns.AsNoTracking()
            .Where(x => x.JobId == job.Id && x.WorkspaceId == workspaceId)
            .OrderByDescending(x => x.CreatedAt)
            .Select(x => (Guid?)x.Id)
            .FirstOrDefaultAsync(cancellationToken);
        var answered = runId is null
            ? 0
            : await db.PolicyEvalAnswers.AsNoTracking().CountAsync(x => x.RunId == runId, cancellationToken);
        var status = string.IsNullOrWhiteSpace(job.EvaluationStatus) ? PolicyEvaluationStatus.NotRun : job.EvaluationStatus;
        return new GetPolicyEvalStatusResult(job.Id, status, job.EvaluationFailedCount, answered);
    }
}
