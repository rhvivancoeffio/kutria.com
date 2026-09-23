using MediatR;
using Commerce.Application.Abstracts;
using Commerce.Application.Features.Brains.GetBrainJob;
using Commerce.Application.Features.Brains.PublishBrainDocument;
using Commerce.Application.Features.Brains.UploadBrainDocument;

namespace Commerce.Infrastructure.Brains;

public sealed class BrainPipeline(IMediator mediator) : IBrainPipeline
{
    public async Task<BrainJobResult> UploadAsync(BrainUploadRequest request, CancellationToken cancellationToken = default)
    {
        var result = await mediator.Send(
            new UploadBrainDocumentCommand(
                request.BrainKey,
                request.FileName,
                request.ContentType,
                request.Content,
                request.MetadataJson),
            cancellationToken);
        return Map(result.Id, result.BrainKey, result.Status, result.FileName, result.PageCount, result.TableCount, result.Text, result.Summary, result.Error);
    }

    public async Task<BrainJobResult?> GetAsync(Guid jobId, CancellationToken cancellationToken = default)
    {
        var result = await mediator.Send(new GetBrainJobQuery(jobId), cancellationToken);
        return result is null
            ? null
            : Map(result.Id, result.BrainKey, result.Status, result.FileName, result.PageCount, result.TableCount, result.Text, result.Summary, result.Error);
    }

    public async Task<BrainJobResult> PublishAsync(Guid jobId, string? editedText, CancellationToken cancellationToken = default)
    {
        var result = await mediator.Send(new PublishBrainDocumentCommand(jobId, editedText), cancellationToken);
        return Map(result.Id, result.BrainKey, result.Status, result.FileName, result.PageCount, result.TableCount, result.Text, result.Summary, result.Error);
    }

    private static BrainJobResult Map(
        Guid id,
        string brainKey,
        string status,
        string fileName,
        int? pageCount,
        int? tableCount,
        string? text,
        string? summary,
        string? error)
        => new(id, brainKey, status, fileName, pageCount, tableCount, text, summary, error);
}
