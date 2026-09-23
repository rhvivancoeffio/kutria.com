using System.Text;
using System.Text.Json;
using Finbuckle.MultiTenant.Abstractions;
using Commerce.Application.Abstracts;
using Commerce.Application.Common;
using Commerce.Application.Common.Abstracts;
using Commerce.Domain.Brains;
using Commerce.Domain.Tenants;

namespace Commerce.Application.Features.Brains.UploadBrainDocument;

public sealed class UploadBrainDocumentHandler(
    ICommerceDbContext db,
    IBrainRawStore store,
    IMessageQueue queue,
    IMultiTenantContextAccessor<CommerceTenantInfo> tenantAccessor,
    IWorkspaceContext workspaceContext)
    : ICommandHandler<UploadBrainDocumentCommand, UploadBrainDocumentResult>
{
    public async Task<UploadBrainDocumentResult> Handle(UploadBrainDocumentCommand request, CancellationToken cancellationToken)
    {
        var tenant = tenantAccessor.MultiTenantContext?.TenantInfo
            ?? throw new InvalidOperationException("Tenant is required.");
        var workspaceId = await WorkspaceRequirement.RequireWorkspaceIdAsync(
            workspaceContext, db, cancellationToken);

        var job = new BrainIngestJob
        {
            WorkspaceId = workspaceId,
            BrainKey = request.BrainKey.Trim(),
            FileName = Path.GetFileName(request.FileName),
            Status = BrainIngestJob.Queued,
            MetadataJson = string.IsNullOrWhiteSpace(request.MetadataJson) ? "{}" : request.MetadataJson
        };
        job.StorageKey = await store.SaveAsync(
            tenant.Id ?? tenant.Identifier ?? "tenant",
            workspaceId,
            job.BrainKey,
            job.Id,
            job.FileName,
            request.Content,
            cancellationToken);
        db.BrainIngestJobs.Add(job);
        await db.SaveChangesAsync(cancellationToken);

        var message = new BrainIngestMessage(job.Id, TenantSlug(tenant), BrainIngestMessage.Parse);
        var body = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(message));
        await queue.SendAsync(BrainQueues.Ingest, body, cancellationToken);
        return BrainJobResults.From(job);
    }

    internal static string TenantSlug(CommerceTenantInfo tenant)
        => string.IsNullOrWhiteSpace(tenant.Identifier) ? tenant.Id ?? string.Empty : tenant.Identifier;
}
