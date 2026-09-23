using Commerce.Application.Abstracts;

namespace Commerce.Infrastructure.Vectors.Common;

public sealed class EmptyCatalogBrainIndex : ICatalogBrainIndex, ICatalogBrainWriter
{
    public Task<IReadOnlyList<CatalogBrainHit>> SearchAsync(
        string tenantId,
        string text,
        int limit,
        CancellationToken cancellationToken = default,
        IReadOnlyList<CatalogOptionFilter>? optionFilters = null)
        => Task.FromResult<IReadOnlyList<CatalogBrainHit>>([]);

    public Task UpsertAsync(string tenantId, string sku, string meaning, CancellationToken cancellationToken = default)
        => throw new InvalidOperationException(
            "Catalog brain embeddings are not configured. Set AzureOpenAI:EmbeddingDeployment and Vector:Provider.");

    public Task UpsertAsync(CatalogProductDocument document, CancellationToken cancellationToken = default)
        => UpsertAsync(document.TenantId, document.Sku, document.Description, cancellationToken);

    public Task UpsertImageDocumentAsync(
        CatalogImageDocument document,
        CancellationToken cancellationToken = default)
        => throw new InvalidOperationException(
            "Catalog brain embeddings are not configured. Set AzureOpenAI:EmbeddingDeployment and Vector:Provider.");
}

public sealed class NoOpVectorIndexBootstrapper : IVectorIndexBootstrapper
{
    public Task EnsureAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
}
