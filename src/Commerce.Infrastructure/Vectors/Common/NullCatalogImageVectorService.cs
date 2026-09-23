using Commerce.Application.Abstracts;

namespace Commerce.Infrastructure.Vectors.Common;

/// <summary>No-op when embeddings / vision are not configured.</summary>
public sealed class NullCatalogImageVectorService : ICatalogImageVectorService
{
    public Task<CatalogImageVectorResult> ResolveAsync(
        string tenantId,
        string? imageUrl,
        CancellationToken cancellationToken = default)
        => Task.FromResult(CatalogImageVectorResult.Empty);
}
