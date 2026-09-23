using System.Text.Json;
using Finbuckle.MultiTenant.Abstractions;
using Microsoft.EntityFrameworkCore;
using Commerce.Application.Abstracts;
using Commerce.Application.Common;
using Commerce.Application.Common.Abstracts;
using Commerce.Domain.Brains;
using Commerce.Domain.Tenants;

namespace Commerce.Application.Features.Policies.ListPublishedPolicies;

public sealed class ListPublishedPoliciesHandler(
    ICommerceDbContext db,
    IBrainRawStore store,
    IMultiTenantContextAccessor<CommerceTenantInfo> tenantAccessor,
    IWorkspaceContext workspaceContext)
    : IQueryHandler<ListPublishedPoliciesQuery, ListPublishedPoliciesResult>
{
    public async Task<ListPublishedPoliciesResult> Handle(
        ListPublishedPoliciesQuery request,
        CancellationToken cancellationToken)
    {
        _ = tenantAccessor.MultiTenantContext?.TenantInfo
            ?? throw new InvalidOperationException("Tenant is required.");
        var workspaceId = await WorkspaceRequirement.RequireWorkspaceIdAsync(
            workspaceContext, db, cancellationToken);

        var jobs = await db.BrainIngestJobs.AsNoTracking()
            .Where(x => x.BrainKey == BrainKeys.Policy
                        && x.WorkspaceId == workspaceId
                        && x.Status == BrainIngestJob.Published)
            .OrderByDescending(x => x.CreatedAt)
            .ToListAsync(cancellationToken);

        var latestByType = new Dictionary<string, PublishedPolicyDocument>(StringComparer.OrdinalIgnoreCase);
        foreach (var job in jobs)
        {
            var type = Read(job.MetadataJson, "type");
            if (string.IsNullOrWhiteSpace(type) || latestByType.ContainsKey(type))
            {
                continue;
            }

            latestByType[type] = new PublishedPolicyDocument(
                job.Id,
                type,
                job.FileName,
                Read(job.MetadataJson, "effective_from"),
                job.CreatedAt,
                job.PageCount,
                job.TableCount,
                job.Summary,
                await SizeAsync(store, job.StorageKey, cancellationToken),
                string.IsNullOrWhiteSpace(job.EvaluationStatus) ? PolicyEvaluationStatus.NotRun : job.EvaluationStatus,
                job.EvaluationFailedCount);
        }

        return new ListPublishedPoliciesResult(latestByType.Values.ToList());
    }

    private static async Task<long?> SizeAsync(IBrainRawStore store, string storageKey, CancellationToken cancellationToken)
    {
        try
        {
            var bytes = await store.ReadAsync(storageKey, cancellationToken);
            return bytes.LongLength;
        }
        catch
        {
            return null;
        }
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
