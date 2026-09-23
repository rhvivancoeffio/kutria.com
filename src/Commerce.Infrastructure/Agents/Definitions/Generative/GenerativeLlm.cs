using System.Text;
using System.Text.Json;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using OpenAI.Chat;
using Commerce.Infrastructure.Vectors.Common;
using ChatMessage = OpenAI.Chat.ChatMessage;

namespace Commerce.Infrastructure.Agents.Definitions.Generative;

internal static class GenerativeLlm
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    public static async Task<string> CompleteJsonAsync(
        IServiceProvider services,
        string system,
        string user,
        CancellationToken cancellationToken)
    {
        var config = services.GetRequiredService<IConfiguration>();
        var endpoint = config["AzureOpenAI:Endpoint"];
        var apiKey = config["AzureOpenAI:ApiKey"];
        var deployment = config["AzureOpenAI:Deployment"] ?? "gpt-4o-mini";
        if (string.IsNullOrWhiteSpace(endpoint) || string.IsNullOrWhiteSpace(apiKey))
            return """{"error":"AzureOpenAI is not configured"}""";

        var chat = AzureOpenAiCompatibleClient.Create(endpoint, apiKey).GetChatClient(deployment);
        var completion = await chat.CompleteChatAsync(
            [
                new SystemChatMessage(system),
                new UserChatMessage(user)
            ],
            cancellationToken: cancellationToken);

        var text = completion.Value.Content.Count > 0
            ? completion.Value.Content[0].Text?.Trim()
            : null;
        return string.IsNullOrWhiteSpace(text) ? """{"error":"empty model response"}""" : ExtractJson(text);
    }

    public static async Task<string> CompleteJsonWithImageAsync(
        IServiceProvider services,
        string system,
        string user,
        BinaryData imageBytes,
        string mime,
        CancellationToken cancellationToken)
    {
        var config = services.GetRequiredService<IConfiguration>();
        var endpoint = config["AzureOpenAI:Endpoint"];
        var apiKey = config["AzureOpenAI:ApiKey"];
        var deployment = config["AzureOpenAI:Deployment"] ?? "gpt-4o-mini";
        if (string.IsNullOrWhiteSpace(endpoint) || string.IsNullOrWhiteSpace(apiKey))
            return """{"error":"AzureOpenAI is not configured"}""";

        var mediaType = string.IsNullOrWhiteSpace(mime) ? "image/jpeg" : mime.Trim();
        if (!mediaType.StartsWith("image/", StringComparison.OrdinalIgnoreCase)
            || mediaType.Contains("svg", StringComparison.OrdinalIgnoreCase))
            return $"{{\"error\":\"invalid image mime '{mediaType}'\"}}";

        var chat = AzureOpenAiCompatibleClient.Create(endpoint, apiKey).GetChatClient(deployment);
        var completion = await chat.CompleteChatAsync(
            [
                new SystemChatMessage(system),
                new UserChatMessage(
                    ChatMessageContentPart.CreateTextPart(user),
                    ChatMessageContentPart.CreateImagePart(imageBytes, mediaType))
            ],
            cancellationToken: cancellationToken);

        var text = completion.Value.Content.Count > 0
            ? completion.Value.Content[0].Text?.Trim()
            : null;
        return string.IsNullOrWhiteSpace(text) ? """{"error":"empty model response"}""" : ExtractJson(text);
    }

    public static string ExtractJson(string text)
    {
        var start = text.IndexOf('{');
        var end = text.LastIndexOf('}');
        if (start < 0 || end <= start)
            return text;
        return text[start..(end + 1)];
    }

    public static string WrapFichaAgentOutput(string fichaJson, string message)
    {
        using var doc = JsonDocument.Parse(string.IsNullOrWhiteSpace(fichaJson) ? "{}" : fichaJson);
        var root = doc.RootElement;
        if (root.TryGetProperty("error", out _))
            return fichaJson;

        var wrapped = new Dictionary<string, object?>
        {
            ["type"] = "message",
            ["message"] = message,
            ["ficha"] = JsonSerializer.Deserialize<object>(fichaJson, Json)
        };
        return JsonSerializer.Serialize(wrapped, Json);
    }

    public static string ModesPrompt(IReadOnlyList<string>? modes)
    {
        if (modes is null || modes.Count == 0
            || modes.Any(m => string.Equals(m, "all", StringComparison.OrdinalIgnoreCase)))
            return "Generate ALL fields: name, description, bullets (3-6), seo.title, seo.metaDescription, brandHint, brandId, isNewBrand, categoryHint, categoryId, isNewCategory.";

        var sb = new StringBuilder("Generate ONLY these fields (others empty string or empty array): ");
        sb.Append(string.Join(", ", modes));
        sb.Append('.');
        return sb.ToString();
    }
}
