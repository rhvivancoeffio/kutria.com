using Commerce.Application.Features.Brains.GetBrainJob;
using Commerce.Application.Features.Brains.PublishBrainDocument;
using Commerce.Application.Features.Brains.UploadBrainDocument;
using Commerce.Domain.Brains;

namespace Commerce.Application.Features.Brains;

internal static class BrainJobResults
{
    public static UploadBrainDocumentResult From(BrainIngestJob job)
        => new(job.Id, job.BrainKey, job.Status, job.FileName, job.PageCount, job.TableCount, Text(job), job.Summary, job.Error);

    public static GetBrainJobResult Get(BrainIngestJob job)
        => new(job.Id, job.BrainKey, job.Status, job.FileName, job.PageCount, job.TableCount, Text(job), job.Summary, job.Error);

    public static PublishBrainDocumentResult Publish(BrainIngestJob job)
        => new(job.Id, job.BrainKey, job.Status, job.FileName, job.PageCount, job.TableCount, Text(job), job.Summary, job.Error);

    private static string? Text(BrainIngestJob job)
        => job.ConfirmedText ?? job.ExtractedText;
}
