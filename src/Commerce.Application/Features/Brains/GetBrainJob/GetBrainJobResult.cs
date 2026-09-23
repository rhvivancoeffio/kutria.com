namespace Commerce.Application.Features.Brains.GetBrainJob;

public sealed record GetBrainJobResult(
    Guid Id,
    string BrainKey,
    string Status,
    string FileName,
    int? PageCount,
    int? TableCount,
    string? Text,
    string? Summary,
    string? Error);
