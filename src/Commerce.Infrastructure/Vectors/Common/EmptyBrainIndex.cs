using Commerce.Application.Abstracts;

namespace Commerce.Infrastructure.Vectors.Common;

public sealed class EmptyBrainIndex : IBrainIndex
{
    public Task UpsertAsync(
        string collectionName,
        IReadOnlyList<BrainPoint> points,
        CancellationToken cancellationToken = default)
        => throw new InvalidOperationException("Brain embeddings are not configured. Set AzureOpenAI:EmbeddingDeployment and Qdrant.");

    public Task<IReadOnlyList<BrainHit>> SearchAsync(BrainSearch search, CancellationToken cancellationToken = default)
        => Task.FromResult<IReadOnlyList<BrainHit>>([]);

    public Task SetPayloadAsync(
        string collectionName,
        IReadOnlyDictionary<string, string> match,
        IReadOnlyDictionary<string, string> payload,
        CancellationToken cancellationToken = default)
        => throw new InvalidOperationException("Brain embeddings are not configured. Set AzureOpenAI:EmbeddingDeployment and Qdrant.");

    public Task DeleteMatchingAsync(
        string collectionName,
        IReadOnlyDictionary<string, string> match,
        IReadOnlyDictionary<string, string>? except,
        CancellationToken cancellationToken = default)
        => throw new InvalidOperationException("Brain embeddings are not configured. Set AzureOpenAI:EmbeddingDeployment and Qdrant.");
}
