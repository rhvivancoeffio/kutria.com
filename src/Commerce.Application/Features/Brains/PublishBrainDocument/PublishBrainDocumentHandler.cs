using System.Text;
using System.Text.Json;
using Finbuckle.MultiTenant.Abstractions;
using Microsoft.EntityFrameworkCore;
using Commerce.Application.Abstracts;
using Commerce.Application.Common;
using Commerce.Application.Common.Abstracts;
using Commerce.Domain.Brains;
using Commerce.Domain.Tenants;

namespace Commerce.Application.Features.Brains.PublishBrainDocument;

public sealed class PublishBrainDocumentHandler(
    ICommerceDbContext db,
    IMessageQueue queue,
    IMultiTenantContextAccessor<CommerceTenantInfo> tenantAccessor,
    IWorkspaceContext workspaceContext)
    : ICommandHandler<PublishBrainDocumentCommand, PublishBrainDocumentResult>
{
    public async Task<PublishBrainDocumentResult> Handle(
        PublishBrainDocumentCommand request,
        CancellationToken cancellationToken)
    {
        var tenant = tenantAccessor.MultiTenantContext?.TenantInfo
            ?? throw new InvalidOperationException("Tenant is required.");
        var workspaceId = await WorkspaceRequirement.RequireWorkspaceIdAsync(
            workspaceContext, db, cancellationToken);

        var job = await db.BrainIngestJobs
                .FirstOrDefaultAsync(x => x.Id == request.JobId && x.WorkspaceId == workspaceId, cancellationToken)
            ?? throw new InvalidOperationException("Brain job was not found.");

        if (!string.Equals(job.Status, BrainIngestJob.Ready, StringComparison.Ordinal))
        {
            throw new InvalidOperationException("Only a ready document can be published.");
        }

        if (request.EditedText is not null)
        {
            job.ConfirmedText = request.EditedText;
        }

        await db.SaveChangesAsync(cancellationToken);
        var message = new BrainIngestMessage(job.Id, UploadBrainDocument.UploadBrainDocumentHandler.TenantSlug(tenant), BrainIngestMessage.Publish);
        var body = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(message));
        await queue.SendAsync(BrainQueues.Ingest, body, cancellationToken);
        return BrainJobResults.Publish(job);
    }
}
