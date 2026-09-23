using OpenAI.Embeddings;

namespace Commerce.Infrastructure.Vectors.Common;

public interface ITextEmbeddingService
{
    Task<float[]> EmbedAsync(string text, CancellationToken cancellationToken = default);
}

public sealed class AzureOpenAiTextEmbeddingService : ITextEmbeddingService
{
    private readonly EmbeddingClient _client;

    public AzureOpenAiTextEmbeddingService(string endpoint, string apiKey, string deployment)
    {
        _client = AzureOpenAiCompatibleClient.Create(endpoint, apiKey)
            .GetEmbeddingClient(deployment);
    }

    public async Task<float[]> EmbedAsync(string text, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(text))
            return [];

        var embedding = await _client.GenerateEmbeddingAsync(text, cancellationToken: cancellationToken);
        return embedding.Value.ToFloats().ToArray();
    }
}
