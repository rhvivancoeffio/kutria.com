using System.Text.Json;
using Finbuckle.MultiTenant.Abstractions;
using Microsoft.EntityFrameworkCore;
using Commerce.Application.Abstracts;
using Commerce.Application.Common.Abstracts;
using Commerce.Domain.Brains;
using Commerce.Domain.Policies;
using Commerce.Domain.Tenants;

namespace Commerce.Application.Features.Policies.RunPolicyEval;

public sealed class RunPolicyEvalHandler(
    ICommerceDbContext db,
    IBrainIndex index,
    IBrainProfileRegistry profiles,
    IPolicyEvalCatalog catalog,
    IPolicyEvalSearch search,
    IPolicyEvalModel model,
    IEventStreamStore eventStreams,
    IMultiTenantContextAccessor<CommerceTenantInfo> tenantAccessor)
    : ICommandHandler<RunPolicyEvalCommand, RunPolicyEvalResult>
{
    public async Task<RunPolicyEvalResult> Handle(RunPolicyEvalCommand request, CancellationToken cancellationToken)
    {
        var tenant = tenantAccessor.MultiTenantContext?.TenantInfo
            ?? throw new InvalidOperationException("Tenant is required.");
        var tenantId = tenant.Id ?? tenant.Identifier ?? string.Empty;
        var streamId = EventStreamKeys.ForPolicyEval(request.JobId);
        var job = await db.BrainIngestJobs.FirstOrDefaultAsync(x => x.Id == request.JobId, cancellationToken)
            ?? throw new InvalidOperationException("Policy job was not found.");
        var type = Read(job.MetadataJson, "type");
        var set = await catalog.ResolveAsync(type, job.WorkspaceId, cancellationToken);
        var run = new PolicyEvalRun
        {
            WorkspaceId = job.WorkspaceId,
            JobId = job.Id,
            TemplateVersion = set.Version,
            Status = PolicyEvalRunStatus.Pending
        };
        db.PolicyEvalRuns.Add(run);
        await db.SaveChangesAsync(cancellationToken);

        await AppendEvalAsync(
            tenantId,
            streamId,
            EventStreamKeys.TypeStatus,
            "En curso.",
            job.Id,
            PolicyEvaluationStatus.Pending,
            0,
            0,
            cancellationToken);

        var failed = 0;
        var answered = 0;
        var blocked = !model.IsAvailable || set.Questions.Count == 0;
        try
        {
        if (!blocked)
        {
            var profile = profiles.Get(BrainKeys.Policy);
            foreach (var question in set.Questions)
            {
                var tool = await search.SearchCandidateAsync(
                    tenantId,
                    job.WorkspaceId,
                    job.Id,
                    question.Question,
                    type,
                    cancellationToken);
                var answer = await model.AnswerAsync(question.Question, tool, cancellationToken);
                var verdict = await model.JudgeAsync(question.Question, tool, answer, cancellationToken);
                if (verdict.Invented)
                {
                    failed++;
                }

                answered++;
                db.PolicyEvalAnswers.Add(new PolicyEvalAnswer
                {
                    RunId = run.Id,
                    QuestionId = question.Id,
                    Question = question.Question,
                    ToolResultJson = tool,
                    Answer = answer,
                    Invented = verdict.Invented,
                    JudgeReason = verdict.Reason
                });
                job.EvaluationFailedCount = failed;
                await db.SaveChangesAsync(cancellationToken);

                await AppendEvalAsync(
                    tenantId,
                    streamId,
                    EventStreamKeys.TypeProgress,
                    $"En curso: {answered} preguntas, {failed} fallos.",
                    job.Id,
                    PolicyEvaluationStatus.Pending,
                    failed,
                    answered,
                    cancellationToken);
            }

            var passed = failed <= PolicyEvaluationStatus.MaxFailed;
            if (passed)
            {
                var match = new Dictionary<string, string>
                {
                    ["tenant_id"] = tenantId,
                    ["workspace_id"] = job.WorkspaceId?.ToString("D") ?? string.Empty,
                    ["job_id"] = job.Id.ToString()
                };
                await index.SetPayloadAsync(profile.CollectionName, match, new Dictionary<string, string> { ["is_evaluated"] = "true" }, cancellationToken);
                if (!string.IsNullOrWhiteSpace(type))
                {
                    await index.DeleteMatchingAsync(
                        profile.CollectionName,
                        new Dictionary<string, string>
                        {
                            ["tenant_id"] = tenantId,
                            ["workspace_id"] = job.WorkspaceId?.ToString("D") ?? string.Empty,
                            ["type"] = type
                        },
                        new Dictionary<string, string> { ["job_id"] = job.Id.ToString() },
                        cancellationToken);
                }
            }

            run.Status = passed ? PolicyEvalRunStatus.Passed : PolicyEvalRunStatus.ReviewFailed;
            run.FailedCount = failed;
            job.EvaluationStatus = passed ? PolicyEvaluationStatus.Passed : PolicyEvaluationStatus.ReviewFailed;
            job.EvaluationFailedCount = failed;
        }
        else
        {
            run.Status = PolicyEvalRunStatus.ReviewFailed;
            run.FailedCount = set.Questions.Count == 0 ? 0 : set.Questions.Count;
            job.EvaluationStatus = PolicyEvaluationStatus.ReviewFailed;
            job.EvaluationFailedCount = run.FailedCount;
            failed = run.FailedCount;
        }
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            run.Status = PolicyEvalRunStatus.ReviewFailed;
            run.FailedCount = failed;
            job.EvaluationStatus = PolicyEvaluationStatus.ReviewFailed;
            job.EvaluationFailedCount = failed;
            job.Error = ex.Message;
            await AppendEvalAsync(
                tenantId,
                streamId,
                EventStreamKeys.TypeError,
                ex.Message,
                job.Id,
                job.EvaluationStatus,
                failed,
                answered,
                cancellationToken);
            run.FinishedAt = DateTimeOffset.UtcNow;
            await db.SaveChangesAsync(cancellationToken);
            return new RunPolicyEvalResult(job.Id, job.EvaluationStatus, failed);
        }

        run.FinishedAt = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync(cancellationToken);

        var terminalType = string.Equals(job.EvaluationStatus, PolicyEvaluationStatus.Passed, StringComparison.Ordinal)
            ? EventStreamKeys.TypeDone
            : EventStreamKeys.TypeError;
        var terminalText = string.Equals(job.EvaluationStatus, PolicyEvaluationStatus.Passed, StringComparison.Ordinal)
            ? $"Aprobado. {failed} fallos."
            : $"Review Failed. {failed} fallos.";
        await AppendEvalAsync(
            tenantId,
            streamId,
            terminalType,
            terminalText,
            job.Id,
            job.EvaluationStatus,
            failed,
            answered,
            cancellationToken);

        return new RunPolicyEvalResult(job.Id, job.EvaluationStatus, failed);
    }

    private async Task AppendEvalAsync(
        string tenantId,
        string streamId,
        string type,
        string text,
        Guid jobId,
        string evaluationStatus,
        int failedCount,
        int answered,
        CancellationToken cancellationToken)
    {
        var data = JsonSerializer.Serialize(new
        {
            id = jobId,
            evaluationStatus,
            failedCount,
            answered,
            message = text
        });
        await eventStreams.AppendAsync(tenantId, streamId, type, text, data, cancellationToken);
    }

    private static string Read(string json, string name)
    {
        try
        {
            using var document = JsonDocument.Parse(string.IsNullOrWhiteSpace(json) ? "{}" : json);
            return document.RootElement.TryGetProperty(name, out var value) ? value.ToString() : string.Empty;
        }
        catch (JsonException)
        {
            return string.Empty;
        }
    }
}
