using System.Text.Json;
using Commerce.Application.Abstracts;

namespace Commerce.Infrastructure.Agents.Tools;

public sealed class CatalogTools(IVectorStore vectors, IEmbeddingGenerator embeddings) : ICatalogTools
{
    public async Task<string> SearchSemanticAsync(string tenantId, string query, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(tenantId))
        {
            throw new InvalidOperationException("tenantId is required for semantic search.");
        }

        var vector = await embeddings.EmbedAsync(query, cancellationToken);
        var hits = await vectors.SearchAsync(tenantId, vector, limit: 5, cancellationToken);
        return JsonSerializer.Serialize(hits);
    }

    public Task<string> GetProductAsync(string productId, CancellationToken cancellationToken = default)
        => NotImplemented(nameof(GetProductAsync));

    public Task<string> CheckStockAsync(string productId, CancellationToken cancellationToken = default)
        => NotImplemented(nameof(CheckStockAsync));

    public Task<string> AddItemAsync(string productId, int quantity, CancellationToken cancellationToken = default)
        => NotImplemented(nameof(AddItemAsync));

    public Task<string> RemoveItemAsync(string productId, CancellationToken cancellationToken = default)
        => NotImplemented(nameof(RemoveItemAsync));

    public Task<string> GetCartAsync(CancellationToken cancellationToken = default)
        => NotImplemented(nameof(GetCartAsync));

    private static Task<string> NotImplemented(string name)
        => Task.FromResult($"{name} is not implemented. The commerce domain is not in this slice.");
}
