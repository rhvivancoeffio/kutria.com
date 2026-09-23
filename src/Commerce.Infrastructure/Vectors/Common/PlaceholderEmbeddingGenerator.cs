using System.Security.Cryptography;
using System.Text;
using Commerce.Application.Abstracts;

namespace Commerce.Infrastructure.Vectors.Common;

/// <summary>Stable placeholder vectors so startup does not depend on an embedding model.</summary>
public sealed class PlaceholderEmbeddingGenerator : IEmbeddingGenerator
{
    public const int Dimensions = 8;

    public Task<float[]> EmbedAsync(string text, CancellationToken cancellationToken = default)
    {
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(text ?? string.Empty));
        var vector = new float[Dimensions];
        for (var i = 0; i < Dimensions; i++)
        {
            vector[i] = hash[i] / 255f;
        }

        return Task.FromResult(vector);
    }
}
