using Microsoft.Extensions.AI;
using Microsoft.Extensions.DependencyInjection;
using Commerce.Application.Features.CreateProduct;
using Commerce.Infrastructure.Vectors.Common;

namespace Commerce.Infrastructure.Agents.Definitions.ProductManager;

internal static class ImageAnalyzeTool
{
    public const string Name = "image_analyze";

    public static AITool Create(IServiceProvider services, string tenantId)
    {
        var verbalizer = services.GetRequiredService<IImageVerbalizer>();
        var images = services.GetRequiredService<IOnboardingImageStore>();
        return AIFunctionFactory.Create(
            async (string? image_url, string? hint, CancellationToken cancellationToken) =>
            {
                _ = tenantId;
                var turn = ChatTurnScope.Current;
                if (!string.IsNullOrWhiteSpace(turn?.ImageAttachmentId))
                {
                    var stored = await images.GetAsync(turn.TenantId, turn.ImageAttachmentId, cancellationToken);
                    if (stored is not null)
                    {
                        await using var ms = new MemoryStream(stored.Value.Bytes, writable: false);
                        var fromAttach = await verbalizer.AnalyzeProductAsync(
                            ms,
                            stored.Value.ContentType,
                            hint,
                            cancellationToken);
                        return Serialize(fromAttach);
                    }
                }

                var url = string.IsNullOrWhiteSpace(image_url) ? turn?.ImageUrl : image_url;
                if (string.IsNullOrWhiteSpace(url))
                    return """{"error":"image_url is required"}""";

                var analysis = await verbalizer.AnalyzeProductUrlAsync(url, hint, cancellationToken);
                return Serialize(analysis);
            },
            name: Name,
            description: AgentTools.Description(services, Name));
    }

    private static string Serialize(ProductImageAnalysis? analysis)
    {
        if (analysis is null)
            return """{"error":"image analysis failed"}""";

        return System.Text.Json.JsonSerializer.Serialize(new
        {
            brand = analysis.Brand,
            title = analysis.Title,
            category = analysis.Category
        });
    }
}
