using Finbuckle.MultiTenant.Abstractions;
using Microsoft.EntityFrameworkCore;
using Commerce.Application.Abstracts;
using Commerce.Application.Common;
using Commerce.Application.Common.Abstracts;
using Commerce.Domain.Tenants;

namespace Commerce.Application.Features.Policies.ListPolicyJobEvals;

public sealed class ListPolicyJobEvalsHandler(
    ICommerceDbContext db,
    IMultiTenantContextAccessor<CommerceTenantInfo> tenantAccessor,
    IWorkspaceContext workspaceContext)
    : IQueryHandler<ListPolicyJobEvalsQuery, ListPolicyJobEvalsResult>
{
    public async Task<ListPolicyJobEvalsResult> Handle(ListPolicyJobEvalsQuery request, CancellationToken cancellationToken)
    {
        _ = tenantAccessor.MultiTenantContext?.TenantInfo
            ?? throw new InvalidOperationException("Tenant is required.");
        var workspaceId = await WorkspaceRequirement.RequireWorkspaceIdAsync(
            workspaceContext, db, cancellationToken);

        var jobExists = await db.BrainIngestJobs.AsNoTracking()
            .AnyAsync(x => x.Id == request.JobId && x.WorkspaceId == workspaceId, cancellationToken);
        if (!jobExists)
            return new ListPolicyJobEvalsResult([]);

        var runs = await db.PolicyEvalRuns.AsNoTracking()
            .Where(x => x.JobId == request.JobId && x.WorkspaceId == workspaceId)
            .OrderByDescending(x => x.CreatedAt)
            .Include(x => x.Answers)
            .ToListAsync(cancellationToken);
        var items = runs.Select(run => new PolicyEvalRunRow(
            run.Id,
            run.TemplateVersion,
            run.Status,
            run.FailedCount,
            run.CreatedAt,
            run.FinishedAt,
            run.Answers
                .OrderBy(answer => answer.CreatedAt)
                .Select(answer => new PolicyEvalAnswerRow(
                    answer.QuestionId,
                    answer.Question,
                    answer.ToolResultJson,
                    answer.Answer,
                    answer.Invented,
                    answer.JudgeReason))
                .ToList()))
            .ToList();
        return new ListPolicyJobEvalsResult(items);
    }
}
