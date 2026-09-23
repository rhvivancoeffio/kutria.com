namespace Commerce.Application.Abstracts;

public sealed record VectorPoint(string Id, string TenantId, string Text, IReadOnlyDictionary<string, string> Payload);

public sealed record VectorHit(string Id, float Score, IReadOnlyDictionary<string, string> Payload);

public interface IVectorStore
{
    Task UpsertAsync(VectorPoint point, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<VectorHit>> SearchAsync(
        string tenantId,
        ReadOnlyMemory<float> vector,
        int limit,
        CancellationToken cancellationToken = default);
}

public interface IEmbeddingGenerator
{
    Task<float[]> EmbedAsync(string text, CancellationToken cancellationToken = default);
}
