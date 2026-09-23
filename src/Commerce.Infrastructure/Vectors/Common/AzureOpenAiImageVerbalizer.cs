using System.ClientModel;
using OpenAI.Chat;

namespace Commerce.Infrastructure.Vectors.Common;

public interface IImageVerbalizer
{
    Task<string?> VerbalizeAsync(Stream image, string? contentType = null, CancellationToken cancellationToken = default);

    Task<string?> VerbalizeUrlAsync(string imageUrl, CancellationToken cancellationToken = default);

    Task<ProductImageAnalysis?> AnalyzeProductAsync(
        Stream image,
        string? contentType = null,
        string? hint = null,
        CancellationToken cancellationToken = default);

    Task<ProductImageAnalysis?> AnalyzeProductUrlAsync(
        string imageUrl,
        string? hint = null,
        CancellationToken cancellationToken = default);
}

public sealed record ProductImageAnalysis(string? Brand, string? Title, string? Category);

public sealed class AzureOpenAiImageVerbalizer : IImageVerbalizer
{
    private readonly ChatClient _chat;
    private readonly IHttpClientFactory _httpClientFactory;

    public AzureOpenAiImageVerbalizer(
        string endpoint,
        string apiKey,
        string deployment,
        IHttpClientFactory httpClientFactory)
    {
        _chat = AzureOpenAiCompatibleClient.Create(endpoint, apiKey)
            .GetChatClient(deployment);
        _httpClientFactory = httpClientFactory;
    }

    public async Task<string?> VerbalizeUrlAsync(string imageUrl, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(imageUrl))
            return null;

        try
        {
            var client = _httpClientFactory.CreateClient("CatalogImageFetch");
            using var response = await client.GetAsync(imageUrl, cancellationToken);
            if (!response.IsSuccessStatusCode)
                return null;

            await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
            var contentType = response.Content.Headers.ContentType?.MediaType;
            return await VerbalizeAsync(stream, contentType, cancellationToken);
        }
        catch
        {
            return null;
        }
    }

    public async Task<string?> VerbalizeAsync(
        Stream image,
        string? contentType = null,
        CancellationToken cancellationToken = default)
    {
        if (image.CanSeek)
            image.Position = 0;

        using var ms = new MemoryStream();
        await image.CopyToAsync(ms, cancellationToken);
        if (ms.Length == 0)
            return null;

        var mime = string.IsNullOrWhiteSpace(contentType) ? "image/jpeg" : contentType;
        var bytes = BinaryData.FromBytes(ms.ToArray());

        try
        {
            var completion = await _chat.CompleteChatAsync(
                [
                    new UserChatMessage(
                        ChatMessageContentPart.CreateTextPart(
                            "Describe this product photo for catalog search in one short Spanish sentence. " +
                            "Include product type, color, brand if visible, and distinctive features. No preamble."),
                        ChatMessageContentPart.CreateImagePart(bytes, mime))
                ],
                cancellationToken: cancellationToken);

            var text = completion.Value.Content.Count > 0
                ? completion.Value.Content[0].Text?.Trim()
                : null;
            return string.IsNullOrWhiteSpace(text) ? null : text;
        }
        catch (ClientResultException)
        {
            return null;
        }
    }

    public async Task<ProductImageAnalysis?> AnalyzeProductUrlAsync(
        string imageUrl,
        string? hint = null,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(imageUrl))
            return null;

        try
        {
            var client = _httpClientFactory.CreateClient("CatalogImageFetch");
            using var response = await client.GetAsync(imageUrl, cancellationToken);
            if (!response.IsSuccessStatusCode)
                return null;

            await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
            var contentType = response.Content.Headers.ContentType?.MediaType;
            return await AnalyzeProductAsync(stream, contentType, hint, cancellationToken);
        }
        catch
        {
            return null;
        }
    }

    public async Task<ProductImageAnalysis?> AnalyzeProductAsync(
        Stream image,
        string? contentType = null,
        string? hint = null,
        CancellationToken cancellationToken = default)
    {
        if (image.CanSeek)
            image.Position = 0;

        using var ms = new MemoryStream();
        await image.CopyToAsync(ms, cancellationToken);
        if (ms.Length == 0)
            return null;

        var mime = string.IsNullOrWhiteSpace(contentType) ? "image/jpeg" : contentType;
        var bytes = BinaryData.FromBytes(ms.ToArray());
        var hintText = string.IsNullOrWhiteSpace(hint)
            ? string.Empty
            : $" User hint for the product title: \"{hint.Trim()}\".";

        try
        {
            var completion = await _chat.CompleteChatAsync(
                [
                    new UserChatMessage(
                        ChatMessageContentPart.CreateTextPart(
                            "Analyze this product photo for catalog onboarding. "
                            + "Reply with ONLY compact JSON: {\"brand\":\"...\",\"title\":\"...\",\"category\":\"...\"}. "
                            + "Use Spanish category names when possible. If brand is unknown use \"Genérica\"."
                            + hintText),
                        ChatMessageContentPart.CreateImagePart(bytes, mime))
                ],
                cancellationToken: cancellationToken);

            var text = completion.Value.Content.Count > 0
                ? completion.Value.Content[0].Text?.Trim()
                : null;
            return ParseAnalysis(text);
        }
        catch (ClientResultException)
        {
            return null;
        }
    }

    private static ProductImageAnalysis? ParseAnalysis(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return null;

        var json = text.Trim();
        var start = json.IndexOf('{');
        var end = json.LastIndexOf('}');
        if (start < 0 || end <= start)
            return null;

        json = json[start..(end + 1)];
        try
        {
            using var doc = System.Text.Json.JsonDocument.Parse(json);
            var root = doc.RootElement;
            string? Read(string name)
                => root.TryGetProperty(name, out var p) && p.ValueKind == System.Text.Json.JsonValueKind.String
                    ? p.GetString()
                    : null;
            return new ProductImageAnalysis(Read("brand"), Read("title"), Read("category"));
        }
        catch (System.Text.Json.JsonException)
        {
            return null;
        }
    }
}
