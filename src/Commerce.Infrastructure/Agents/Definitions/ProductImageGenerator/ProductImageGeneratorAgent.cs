using System.Text.Json;
using Finbuckle.MultiTenant.Abstractions;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using OpenAI.Images;
using Commerce.Application.Abstracts;
using Commerce.Application.Features.CreateProduct;
using Commerce.Domain.Tenants;
using Commerce.Infrastructure.Agents.Definitions.Generative;
using Commerce.Infrastructure.Vectors.Common;

namespace Commerce.Infrastructure.Agents.Definitions.ProductImageGenerator;

public sealed class ProductImageGeneratorAgent : IAgentModule
{
    public const string KeyName = "product-image-generator";

    public string Key => KeyName;

    public static IServiceCollection Add(IServiceCollection services)
        => services.AddScoped<IAgentModule, ProductImageGeneratorAgent>();

    public IReadOnlyList<AITool> CreateTools(AgentDefinition definition, string tenantId, IServiceProvider services)
    {
        var available = new Dictionary<string, AITool>(StringComparer.OrdinalIgnoreCase)
        {
            [GenerateProductImagesTool.Name] = GenerateProductImagesTool.Create(services, tenantId)
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

internal static class GenerateProductImagesTool
{
    public const string Name = "generate_product_images";
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    public static AITool Create(IServiceProvider services, string tenantId)
        => AIFunctionFactory.Create(
            async (string name, string? description, int? count, CancellationToken cancellationToken) =>
            {
                if (string.IsNullOrWhiteSpace(name))
                    throw new InvalidOperationException("name is required");

                var config = services.GetRequiredService<IConfiguration>();
                var endpoint = config["AzureOpenAI:Endpoint"];
                var apiKey = config["AzureOpenAI:ApiKey"];
                var imageDeployment = config["AzureOpenAI:ImageDeployment"];
                if (string.IsNullOrWhiteSpace(endpoint) || string.IsNullOrWhiteSpace(apiKey))
                    throw new InvalidOperationException("AzureOpenAI is not configured");
                if (string.IsNullOrWhiteSpace(imageDeployment))
                    throw new InvalidOperationException(
                        "AzureOpenAI:ImageDeployment is not configured (e.g. dall-e-3 or gpt-image-1)");

                var tenant = services.GetService<IMultiTenantContextAccessor<CommerceTenantInfo>>()
                    ?.MultiTenantContext?.TenantInfo;
                var tenantKey = tenant?.Id ?? tenantId;
                var tenantSlug = tenant?.Identifier ?? tenantId;
                if (string.IsNullOrWhiteSpace(tenantKey))
                    throw new InvalidOperationException("tenant is required");

                var n = Math.Clamp(count ?? 1, 1, 2);
                var prompt = BuildCatalogPrompt(name.Trim(), description);
                var imagesStore = services.GetRequiredService<IOnboardingImageStore>();
                var client = AzureOpenAiCompatibleClient.Create(endpoint, apiKey).GetImageClient(imageDeployment);

                var generated = new List<object>();
                for (var i = 0; i < n; i++)
                {
                    var result = await client.GenerateImageAsync(
                        prompt,
                        new OpenAI.Images.ImageGenerationOptions
                        {
                            ResponseFormat = GeneratedImageFormat.Bytes,
                            Size = GeneratedImageSize.W1024xH1024
                        },
                        cancellationToken);

                    var bytes = result.Value.ImageBytes;
                    if (bytes is null || bytes.ToMemory().IsEmpty)
                        throw new InvalidOperationException("image generation returned empty bytes");

                    var attachmentId = $"genimg_{Guid.NewGuid():N}";
                    await imagesStore.SaveAsync(
                        tenantKey,
                        attachmentId,
                        bytes.ToArray(),
                        "image/png",
                        cancellationToken);

                    var relative = $"/t/{tenantSlug}/chat/attachments/{attachmentId}";
                    generated.Add(new
                    {
                        attachmentId,
                        url = relative,
                        prompt
                    });
                }

                return JsonSerializer.Serialize(new
                {
                    type = "message",
                    message = generated.Count == 1
                        ? "Imagen de producto lista."
                        : $"{generated.Count} imágenes de producto listas.",
                    images = generated
                }, Json);
            },
            name: Name,
            description: AgentTools.Description(services, Name));

    private static string BuildCatalogPrompt(string name, string? description)
    {
        var desc = string.IsNullOrWhiteSpace(description)
            ? ""
            : $" Product details: {description.Trim()}.";
        return
            "Professional ecommerce catalog photo of a single product: "
            + name
            + "."
            + desc
            + " Clean white seamless studio background, soft even lighting, product centered, "
            + "sharp focus, high detail, no people, no hands, no text overlay, no watermark, no logo inventado.";
    }
}
