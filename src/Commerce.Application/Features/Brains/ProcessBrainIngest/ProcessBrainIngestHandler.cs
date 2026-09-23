using Finbuckle.MultiTenant.Abstractions;
using Microsoft.EntityFrameworkCore;
using Commerce.Application.Abstracts;
using Commerce.Application.Common.Abstracts;
using Commerce.Domain.Brains;
using Commerce.Domain.Tenants;

namespace Commerce.Application.Features.Brains.ProcessBrainIngest;

public sealed class ProcessBrainIngestHandler(
    ICommerceDbContext db,
    IBrainRawStore store,
    IBrainDocumentParser parser,
    IBrainIndex index,
    IBrainProfileRegistry profiles,
    IMultiTenantContextAccessor<CommerceTenantInfo> tenantAccessor)
    : ICommandHandler<ProcessBrainIngestCommand, ProcessBrainIngestResult>
{
    public const int MaxPublishedDocuments = 200;

    public async Task<ProcessBrainIngestResult> Handle(ProcessBrainIngestCommand request, CancellationToken cancellationToken)
    {
        var tenant = tenantAccessor.MultiTenantContext?.TenantInfo
            ?? throw new InvalidOperationException("Tenant is required.");

        var job = await db.BrainIngestJobs.FirstOrDefaultAsync(x => x.Id == request.JobId, cancellationToken)
            ?? throw new InvalidOperationException("Brain job was not found.");

        if (string.Equals(request.Step, BrainIngestMessage.Parse, StringComparison.Ordinal))
        {
            await ParseAsync(job, cancellationToken);
        }
        else
        {
            await PublishAsync(job, tenant.Id ?? tenant.Identifier ?? string.Empty, cancellationToken);
        }

        return new ProcessBrainIngestResult(job.Id, job.Status);
    }

    private async Task ParseAsync(BrainIngestJob job, CancellationToken cancellationToken)
    {
        if (string.Equals(job.Status, BrainIngestJob.Published, StringComparison.Ordinal))
        {
            return;
        }

        job.Status = BrainIngestJob.Reading;
        await db.SaveChangesAsync(cancellationToken);

        var bytes = await store.ReadAsync(job.StorageKey, cancellationToken);
        var parsed = await parser.ParseAsync(job.FileName, bytes, cancellationToken);
        job.PageCount = parsed.PageCount;
        job.TableCount = parsed.TableCount;

        if (parsed.Unsupported)
        {
            job.Status = BrainIngestJob.Failed;
            job.Error = parsed.Error ?? "This file type is not supported yet.";
            await db.SaveChangesAsync(cancellationToken);
            return;
        }

        if (parsed.NeedsOcr)
        {
            job.Status = BrainIngestJob.NeedsOcr;
            job.Error = "Needs OCR. The original file was kept and was not published.";
            await db.SaveChangesAsync(cancellationToken);
            return;
        }

        job.ExtractedText = parsed.Text;
        job.Summary = FirstLine(parsed.Text);
        job.Error = null;
        job.Status = BrainIngestJob.Ready;
        await db.SaveChangesAsync(cancellationToken);
    }

    private async Task PublishAsync(BrainIngestJob job, string tenantId, CancellationToken cancellationToken)
    {
        if (string.Equals(job.Status, BrainIngestJob.Published, StringComparison.Ordinal))
        {
            return;
        }

        if (!string.Equals(job.Status, BrainIngestJob.Ready, StringComparison.Ordinal)
            && !string.Equals(job.Status, BrainIngestJob.Publishing, StringComparison.Ordinal))
        {
            throw new InvalidOperationException($"Cannot publish a job in status '{job.Status}'.");
        }

        var text = job.ConfirmedText ?? job.ExtractedText;
        if (string.IsNullOrWhiteSpace(text))
        {
            job.Status = BrainIngestJob.Failed;
            job.Error = "There is no text to publish.";
            await db.SaveChangesAsync(cancellationToken);
            return;
        }

        var published = await db.BrainIngestJobs.CountAsync(
            x => x.BrainKey == job.BrainKey
                 && x.WorkspaceId == job.WorkspaceId
                 && x.Status == BrainIngestJob.Published,
            cancellationToken);
        if (published >= MaxPublishedDocuments)
        {
            job.Status = BrainIngestJob.Failed;
            job.Error = "This brain already has 200 published documents.";
            await db.SaveChangesAsync(cancellationToken);
            return;
        }

        IBrainProfile profile;
        try
        {
            profile = profiles.Get(job.BrainKey);
        }
        catch (InvalidOperationException ex)
        {
            job.Status = BrainIngestJob.Failed;
            job.Error = ex.Message;
            await db.SaveChangesAsync(cancellationToken);
            return;
        }

        job.Status = BrainIngestJob.Publishing;
        await db.SaveChangesAsync(cancellationToken);

        var document = new BrainPublishedDocument(
            tenantId,
            job.WorkspaceId,
            job.Id,
            job.FileName,
            store.ToSourceUrl(job.StorageKey),
            text,
            job.MetadataJson);
        try
        {
            await index.UpsertAsync(profile.CollectionName, profile.BuildPoints(document), cancellationToken);
        }
        catch (InvalidOperationException ex)
        {
            job.Status = BrainIngestJob.Failed;
            job.Error = ex.Message;
            await db.SaveChangesAsync(cancellationToken);
            return;
        }

        job.Status = BrainIngestJob.Published;
        job.EvaluationStatus = PolicyEvaluationStatus.NotRun;
        job.EvaluationFailedCount = 0;
        job.Error = null;
        await db.SaveChangesAsync(cancellationToken);
    }

    private static string FirstLine(string text)
    {
        var line = text.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .FirstOrDefault() ?? string.Empty;
        return line.Length <= 180 ? line : line[..180];
    }
}
