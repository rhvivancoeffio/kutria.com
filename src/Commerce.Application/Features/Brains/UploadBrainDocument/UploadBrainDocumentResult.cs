namespace Commerce.Application.Features.Brains.UploadBrainDocument;

public sealed record UploadBrainDocumentResult(
    Guid Id,
    string BrainKey,
    string Status,
    string FileName,
    int? PageCount,
    int? TableCount,
    string? Text,
    string? Summary,
    string? Error);
