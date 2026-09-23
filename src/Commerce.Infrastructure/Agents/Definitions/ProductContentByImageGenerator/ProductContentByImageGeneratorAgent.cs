using Microsoft.Extensions.AI;
using Microsoft.Extensions.DependencyInjection;
using Commerce.Application.Abstracts;
using Commerce.Application.Features.CreateProduct;
using Commerce.Infrastructure.Agents.Definitions.Generative;
using Commerce.Infrastructure.Vectors.Common;

namespace Commerce.Infrastructure.Agents.Definitions.ProductContentByImageGenerator;

public sealed class ProductContentByImageGeneratorAgent : IAgentModule
{
    public const string KeyName = "product-content-by-image-generator";

    public string Key => KeyName;

    public static IServiceCollection Add(IServiceCollection services)
        => services.AddScoped<IAgentModule, ProductContentByImageGeneratorAgent>();

    public IReadOnlyList<AITool> CreateTools(AgentDefinition definition, string tenantId, IServiceProvider services)
    {
        var available = new Dictionary<string, AITool>(StringComparer.OrdinalIgnoreCase)
        {
            [GenerateProductContentFromImageTool.Name] = GenerateProductContentFromImageTool.Create(services, tenantId)
        };
        return AgentTools.Bind(definition, available, services);
    }

    public Task ExecuteWorkflowAsync(
        AgentDefinition definition,
        string payload,
        IServiceProvider services,
        CancellationToken cancellationToken)
        => Task.CompletedTask;
}

internal static class GenerateProductContentFromImageTool
{
    public const string Name = "generate_product_content_from_image";

    private const string AttachmentsPath = "/chat/attachments/";

    public static AITool Create(IServiceProvider services, string tenantId)
    {
        var images = services.GetRequiredService<IOnboardingImageStore>();
        var httpFactory = services.GetRequiredService<IHttpClientFactory>();

        return AIFunctionFactory.Create(
            async (string? image_url, string? hint, CancellationToken cancellationToken) =>
            {
                var resolved = await ResolveImageAsync(
                    images,
                    httpFactory,
                    tenantId,
                    image_url,
                    cancellationToken);
                if (resolved is null)
                    return """{"error":"image is required"}""";
                if (!string.IsNullOrWhiteSpace(resolved.Value.Error))
                    return $"{{\"error\":{System.Text.Json.JsonSerializer.Serialize(resolved.Value.Error)}}}";

                var (catalogBlock, categories) = await GenerativeFichaCatalog.LoadCategoryPromptAsync(
                    services,
                    cancellationToken);

                var system = """
                    Eres catalogador ecommerce Perú. Analiza la foto y responde SOLO JSON:
                    {"name":"","description":"","bullets":[],"seo":{"title":"","metaDescription":""},"brandHint":"","brandId":null,"isNewBrand":true,"categoryHint":"","categoryId":null,"isNewCategory":true}
                    Español comercial, sin markdown. bullets 3-6.
                    SIEMPRE incluye brandHint y categoryHint como strings no vacíos. Nunca uses null en esos hints.
                    Usa CATEGORIAS_GRAVITY del mensaje de usuario para decidir isNewCategory / categoryId.
                    """;
                var userCore = string.IsNullOrWhiteSpace(hint)
                    ? "Genera ficha completa a partir de esta imagen."
                    : $"Hint del vendedor: {hint.Trim()}. Genera ficha completa.";
                var user = userCore + "\n\n" + catalogBlock;
                var ficha = await GenerativeLlm.CompleteJsonWithImageAsync(
                    services,
                    system,
                    user,
                    BinaryData.FromBytes(resolved.Value.Data!),
                    resolved.Value.ContentType ?? "image/jpeg",
                    cancellationToken);
                ficha = await GenerativeFichaCatalog.EnrichFichaJsonAsync(
                    services,
                    ficha,
                    categories,
                    cancellationToken);
                return GenerativeLlm.WrapFichaAgentOutput(ficha, "Ficha generada desde imagen.");
            },
            name: Name,
            description: AgentTools.Description(services, Name));
    }

    private static async Task<(byte[]? Data, string? ContentType, string? Error)?> ResolveImageAsync(
        IOnboardingImageStore images,
        IHttpClientFactory httpFactory,
        string tenantId,
        string? imageUrl,
        CancellationToken cancellationToken)
    {
        var turn = ChatTurnScope.Current;

        // 1) Ambient turn attachment (when AsyncLocal is available).
        if (!string.IsNullOrWhiteSpace(turn?.ImageAttachmentId))
        {
            var fromTurn = await images.GetAsync(
                turn.TenantId,
                turn.ImageAttachmentId,
                cancellationToken);
            if (fromTurn is not null)
                return ValidateImage(fromTurn.Value.Bytes, fromTurn.Value.ContentType);
        }

        var url = string.IsNullOrWhiteSpace(imageUrl) ? turn?.ImageUrl : imageUrl;

        // 2) Parse /chat/attachments/{id} and load from store (LLM often passes the public URL;
        //    AsyncLocal may be empty inside the tool callback, and localhost HTTP returns HTML).
        var attachmentId = TryParseAttachmentId(url) ?? TryParseAttachmentId(turn?.ImageUrl);
        if (!string.IsNullOrWhiteSpace(attachmentId))
        {
            foreach (var tid in DistinctTenantIds(tenantId, turn?.TenantId))
            {
                var stored = await images.GetAsync(tid, attachmentId, cancellationToken);
                if (stored is not null)
                    return ValidateImage(stored.Value.Bytes, stored.Value.ContentType);
            }
        }

        if (string.IsNullOrWhiteSpace(url))
            return null;

        // 3) External / absolute URL fetch — only accept real image payloads.
        try
        {
            var client = httpFactory.CreateClient("CatalogImageFetch");
            using var response = await client.GetAsync(url, cancellationToken);
            if (!response.IsSuccessStatusCode)
                return (null, null, $"image fetch failed ({(int)response.StatusCode})");

            var data = await response.Content.ReadAsByteArrayAsync(cancellationToken);
            if (data.Length == 0)
                return (null, null, "image fetch returned empty body");

            var mediaType = response.Content.Headers.ContentType?.MediaType;
            return ValidateImage(data, mediaType);
        }
        catch (Exception ex)
        {
            return (null, null, $"image fetch error: {ex.Message}");
        }
    }

    private static (byte[]? Data, string? ContentType, string? Error) ValidateImage(
        byte[] data,
        string? contentType)
    {
        var sniffed = SniffImageMime(data);
        var mime = sniffed
            ?? (IsImageContentType(contentType) ? contentType!.Trim() : null);

        if (mime is null)
        {
            var got = string.IsNullOrWhiteSpace(contentType) ? "unknown" : contentType.Trim();
            return (null, null,
                $"image payload is not an image (content-type '{got}'). Prefer attachment store over HTTP for local URLs.");
        }

        return (data, mime, null);
    }

    private static bool IsImageContentType(string? mime)
        => !string.IsNullOrWhiteSpace(mime)
           && mime.StartsWith("image/", StringComparison.OrdinalIgnoreCase)
           && !mime.Contains("svg", StringComparison.OrdinalIgnoreCase);

    private static string? SniffImageMime(byte[] data)
    {
        if (data.Length >= 3 && data[0] == 0xFF && data[1] == 0xD8 && data[2] == 0xFF)
            return "image/jpeg";
        if (data.Length >= 8
            && data[0] == 0x89 && data[1] == 0x50 && data[2] == 0x4E && data[3] == 0x47
            && data[4] == 0x0D && data[5] == 0x0A && data[6] == 0x1A && data[7] == 0x0A)
            return "image/png";
        if (data.Length >= 6
            && data[0] == (byte)'G' && data[1] == (byte)'I' && data[2] == (byte)'F'
            && data[3] == (byte)'8' && (data[4] == (byte)'7' || data[4] == (byte)'9') && data[5] == (byte)'a')
            return "image/gif";
        if (data.Length >= 12
            && data[0] == (byte)'R' && data[1] == (byte)'I' && data[2] == (byte)'F' && data[3] == (byte)'F'
            && data[8] == (byte)'W' && data[9] == (byte)'E' && data[10] == (byte)'B' && data[11] == (byte)'P')
            return "image/webp";
        return null;
    }

    private static string? TryParseAttachmentId(string? url)
    {
        if (string.IsNullOrWhiteSpace(url))
            return null;

        var i = url.IndexOf(AttachmentsPath, StringComparison.OrdinalIgnoreCase);
        if (i < 0)
            return null;

        var id = url[(i + AttachmentsPath.Length)..].Trim();
        var slash = id.IndexOf('/');
        if (slash >= 0)
            id = id[..slash];
        var q = id.IndexOfAny(['?', '#']);
        if (q >= 0)
            id = id[..q];

        return string.IsNullOrWhiteSpace(id) ? null : id;
    }

    private static IEnumerable<string> DistinctTenantIds(params string?[] ids)
    {
        var seen = new HashSet<string>(StringComparer.Ordinal);
        foreach (var id in ids)
        {
            if (string.IsNullOrWhiteSpace(id) || !seen.Add(id))
                continue;
            yield return id;
        }
    }
}
