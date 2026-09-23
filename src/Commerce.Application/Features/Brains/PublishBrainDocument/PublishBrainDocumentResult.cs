namespace Commerce.Application.Features.Brains.PublishBrainDocument;

public sealed record PublishBrainDocumentResult(
    Guid Id,
    string BrainKey,
    string Status,
    string FileName,
    int? PageCount,
    int? TableCount,
    string? Text,
    string? Summary,
    string? Error);
