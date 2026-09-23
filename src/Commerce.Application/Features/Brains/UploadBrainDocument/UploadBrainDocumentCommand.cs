using Commerce.Application.Common.Abstracts;

namespace Commerce.Application.Features.Brains.UploadBrainDocument;

public sealed record UploadBrainDocumentCommand(
    string BrainKey,
    string FileName,
    string ContentType,
    byte[] Content,
    string? MetadataJson) : ICommand<UploadBrainDocumentResult>;
