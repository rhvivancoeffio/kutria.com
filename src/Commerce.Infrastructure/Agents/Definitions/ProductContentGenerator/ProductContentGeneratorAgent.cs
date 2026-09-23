using Microsoft.Extensions.AI;
using Microsoft.Extensions.DependencyInjection;
using Commerce.Application.Abstracts;
using Commerce.Infrastructure.Agents.Definitions.Generative;

namespace Commerce.Infrastructure.Agents.Definitions.ProductContentGenerator;

public sealed class ProductContentGeneratorAgent : IAgentModule
{
    public const string KeyName = "product-content-generator";

    public string Key => KeyName;

    public static IServiceCollection Add(IServiceCollection services)
        => services.AddScoped<IAgentModule, ProductContentGeneratorAgent>();

    public IReadOnlyList<AITool> CreateTools(AgentDefinition definition, string tenantId, IServiceProvider services)
    {
        var available = new Dictionary<string, AITool>(StringComparer.OrdinalIgnoreCase)
        {
            [GenerateProductContentTool.Name] = GenerateProductContentTool.Create(services, tenantId)
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

internal static class GenerateProductContentTool
{
    public const string Name = "generate_product_content";

    public static AITool Create(IServiceProvider services, string tenantId)
        => AIFunctionFactory.Create(
            async (string name, string[]? modes, CancellationToken cancellationToken) =>
            {
                _ = tenantId;
                if (string.IsNullOrWhiteSpace(name))
                    return """{"error":"name is required"}""";

                var modeList = modes?.Where(m => !string.IsNullOrWhiteSpace(m)).Select(m => m.Trim()).ToArray()
                    ?? ["all"];
                var (catalogBlock, categories) = await GenerativeFichaCatalog.LoadCategoryPromptAsync(
                    services,
                    cancellationToken);
                var system = """
                    Eres catalogador ecommerce Perú. Responde SOLO JSON:
                    {"name":"","description":"","bullets":[],"seo":{"title":"","metaDescription":""},"brandHint":"","brandId":null,"isNewBrand":true,"categoryHint":"","categoryId":null,"isNewCategory":true}
                    Español comercial, sin markdown.
                    SIEMPRE incluye brandHint y categoryHint como strings no vacíos. Nunca uses null en esos hints.
                    Usa CATEGORIAS_GRAVITY del mensaje de usuario para decidir isNewCategory / categoryId.
                    """;
                var user =
                    $"Producto: {name.Trim()}\n{GenerativeLlm.ModesPrompt(modeList)}\n\n{catalogBlock}";
                var ficha = await GenerativeLlm.CompleteJsonAsync(services, system, user, cancellationToken);
                ficha = await GenerativeFichaCatalog.EnrichFichaJsonAsync(
                    services,
                    ficha,
                    categories,
                    cancellationToken);
                return GenerativeLlm.WrapFichaAgentOutput(ficha, "Ficha generada.");
            },
            name: Name,
            description: AgentTools.Description(services, Name));
    }
