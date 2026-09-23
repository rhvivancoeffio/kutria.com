using System.ClientModel;
using OpenAI;

namespace Commerce.Infrastructure.Vectors.Common;

/// <summary>
/// Builds an <see cref="OpenAIClient"/> against Azure OpenAI's OpenAI-compatible
/// <c>/openai/v1</c> endpoint. Avoids <c>Azure.AI.OpenAI</c>, which is binary-
/// incompatible with the OpenAI package pulled by Microsoft.Extensions.AI.OpenAI.
/// </summary>
internal static class AzureOpenAiCompatibleClient
{
    public static OpenAIClient Create(string endpoint, string apiKey)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(endpoint);
        ArgumentException.ThrowIfNullOrWhiteSpace(apiKey);

        var root = endpoint.TrimEnd('/');
        if (!root.EndsWith("/openai/v1", StringComparison.OrdinalIgnoreCase))
            root += "/openai/v1";

        return new OpenAIClient(
            new ApiKeyCredential(apiKey),
            new OpenAIClientOptions { Endpoint = new Uri(root + "/") });
    }
}
