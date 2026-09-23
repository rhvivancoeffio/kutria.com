namespace Commerce.Application.Abstracts;

public sealed record BrainParseResult(
    string Text,
    int PageCount,
    int TableCount,
    bool NeedsOcr,
    bool Unsupported,
    string? Error);

public interface IBrainDocumentParser
{
    Task<BrainParseResult> ParseAsync(
        string fileName,
        ReadOnlyMemory<byte> content,
        CancellationToken cancellationToken = default);
}
