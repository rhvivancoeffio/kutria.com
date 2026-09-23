using System.Text;
using System.Text.Json;
using Finbuckle.MultiTenant.Abstractions;
using Microsoft.EntityFrameworkCore;
using Commerce.Application.Abstracts;
using Commerce.Application.Common;
using Commerce.Application.Common.Abstracts;
using Commerce.Domain.Brains;
using Commerce.Domain.Tenants;

namespace Commerce.Application.Features.Policies.StartPolicyEval;

public sealed class StartPolicyEvalHandler(
    ICommerceDbContext db,
    IMessageQueue queue,
    IEventStreamStore eventStreams,
    IMultiTenantContextAccessor<CommerceTenantInfo> tenantAccessor,
    IWorkspaceContext workspaceContext)
    : ICommandHandler<StartPolicyEvalCommand, StartPolicyEvalResult>
{
    public async Task<StartPolicyEvalResult> Handle(StartPolicyEvalCommand request, CancellationToken cancellationToken)
    {
        var tenant = tenantAccessor.MultiTenantContext?.TenantInfo
            ?? throw new InvalidOperationException("Tenant is required.");
        var tenantId = tenant.Id ?? throw new InvalidOperationException("Tenant is required.");
        var workspaceId = await WorkspaceRequirement.RequireWorkspaceIdAsync(
            workspaceContext, db, cancellationToken);
        var job = await db.BrainIngestJobs
                .FirstOrDefaultAsync(x => x.Id == request.JobId && x.WorkspaceId == workspaceId, cancellationToken)
            ?? throw new InvalidOperationException("Policy job was not found.");
        if (!string.Equals(job.BrainKey, BrainKeys.Policy, StringComparison.OrdinalIgnoreCase)
            || !string.Equals(job.Status, BrainIngestJob.Published, StringComparison.Ordinal))
        {
            throw new InvalidOperationException("Only a published policy can be evaluated.");
        }

        var streamId = EventStreamKeys.ForPolicyEval(job.Id);

        if (string.Equals(job.EvaluationStatus, PolicyEvaluationStatus.Pending, StringComparison.Ordinal))
        {
            return new StartPolicyEvalResult(job.Id, job.EvaluationStatus, false, streamId);
        }

        job.EvaluationStatus = PolicyEvaluationStatus.Pending;
        await db.SaveChangesAsync(cancellationToken);
        var slug = string.IsNullOrWhiteSpace(tenant.Identifier) ? tenant.Id ?? string.Empty : tenant.Identifier;
        var message = new PolicyEvalMessage(job.Id, slug);
        await queue.SendAsync(BrainQueues.PolicyEval, Encoding.UTF8.GetBytes(JsonSerializer.Serialize(message)), cancellationToken);

        await eventStreams.AppendAsync(
            tenantId,
            streamId,
            EventStreamKeys.TypeStatus,
            "En cola.",
            JsonSerializer.Serialize(new
            {
                id = job.Id,
                evaluationStatus = PolicyEvaluationStatus.Pending,
                failedCount = 0,
                answered = 0,
                message = "En cola."
            }),
            cancellationToken);

        return new StartPolicyEvalResult(job.Id, job.EvaluationStatus, true, streamId);
    }
}
