using Finbuckle.MultiTenant.Abstractions;
using Microsoft.EntityFrameworkCore;
using Commerce.Application.Abstracts;
using Commerce.Application.Common;
using Commerce.Application.Common.Abstracts;
using Commerce.Domain.Brains;
using Commerce.Domain.Tenants;

namespace Commerce.Application.Features.Policies.DownloadPolicyFile;

public sealed class DownloadPolicyFileHandler(
    ICommerceDbContext db,
    IBrainRawStore store,
    IMultiTenantContextAccessor<CommerceTenantInfo> tenantAccessor,
    IWorkspaceContext workspaceContext)
    : IQueryHandler<DownloadPolicyFileQuery, DownloadPolicyFileResult?>
{
    public async Task<DownloadPolicyFileResult?> Handle(
        DownloadPolicyFileQuery request,
        CancellationToken cancellationToken)
    {
        _ = tenantAccessor.MultiTenantContext?.TenantInfo
            ?? throw new InvalidOperationException("Tenant is required.");
        var workspaceId = await WorkspaceRequirement.RequireWorkspaceIdAsync(
            workspaceContext, db, cancellationToken);

        var job = await db.BrainIngestJobs.AsNoTracking()
            .FirstOrDefaultAsync(
                x => x.Id == request.JobId
                     && x.WorkspaceId == workspaceId
                     && x.BrainKey == BrainKeys.Policy
                     && x.Status == BrainIngestJob.Published,
                cancellationToken);
        if (job is null)
        {
            return null;
        }

        var content = await store.ReadAsync(job.StorageKey, cancellationToken);
        return new DownloadPolicyFileResult(job.FileName, ContentType(job.FileName), content);
    }

    private static string ContentType(string fileName)
        => Path.GetExtension(fileName).ToLowerInvariant() switch
        {
            ".pdf" => "application/pdf",
            ".docx" => "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
            ".json" => "application/json",
            ".txt" => "text/plain",
            _ => "application/octet-stream"
        };
}
