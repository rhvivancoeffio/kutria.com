namespace Commerce.Application.Abstracts;

public interface IBrainRawStore
{
    Task<string> SaveAsync(
        string tenantId,
        Guid workspaceId,
        string brainKey,
        Guid jobId,
        string fileName,
        ReadOnlyMemory<byte> content,
        CancellationToken cancellationToken = default);

    Task<byte[]> ReadAsync(string storageKey, CancellationToken cancellationToken = default);

    string ToSourceUrl(string storageKey);
}
