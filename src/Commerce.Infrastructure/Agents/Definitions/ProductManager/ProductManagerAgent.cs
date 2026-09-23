using Microsoft.Extensions.AI;
using Microsoft.Extensions.DependencyInjection;
using Commerce.Application.Abstracts;
using Commerce.Infrastructure.Agents.Definitions.BrandManager;
using Commerce.Infrastructure.Agents.Definitions.CategoryManager;
using Commerce.Infrastructure.Agents.GravityCatalog;

namespace Commerce.Infrastructure.Agents.Definitions.ProductManager;

public sealed class ProductManagerAgent : IAgentModule
{
    public const string KeyName = "product-manager";

    public string Key => KeyName;

    public static IServiceCollection Add(IServiceCollection services)
        => services.AddScoped<IAgentModule, ProductManagerAgent>();

    public IReadOnlyList<AITool> CreateTools(AgentDefinition definition, string tenantId, IServiceProvider services)
    {
        var available = new Dictionary<string, AITool>(StringComparer.OrdinalIgnoreCase)
        {
            [ImageAnalyzeTool.Name] = ImageAnalyzeTool.Create(services, tenantId),
            [BrandListTool.Name] = BrandListTool.Create(services, tenantId),
            [BrandCreateTool.Name] = BrandCreateTool.Create(services, tenantId),
            [CategoryListTool.Name] = CategoryListTool.Create(services, tenantId),
            [CategoryCreateTool.Name] = CategoryCreateTool.Create(services, tenantId),
            [ProductCreateTool.Name] = ProductCreateTool.Create(services, tenantId),
            [ProductListTool.Name] = ProductListTool.Create(services, tenantId),
            [ProductUpdateTool.Name] = ProductUpdateTool.Create(services, tenantId),
            [ProductDeleteTool.Name] = ProductDeleteTool.Create(services, tenantId),
            [ProductPublishTool.Name] = ProductPublishTool.Create(services, tenantId)
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

internal static class ProductCreateTool
{
    public const string Name = "product_create";

    public static AITool Create(IServiceProvider services, string tenantId)
        => AIFunctionFactory.Create(
            async (
                string title,
                string? description,
                string? brand_id,
                string? brand_name,
                string? category_id,
                string? category_path,
                string? image_url,
                CancellationToken cancellationToken) =>
            {
                _ = tenantId;
                var (client, credentials) = await GravityAgentContext.ResolveAsync(services, cancellationToken);
                var resolvedImage = string.IsNullOrWhiteSpace(image_url)
                    ? ChatTurnScope.Current?.ImageUrl
                    : image_url;
                var created = await client.CreateProductAsync(
                    credentials,
                    new GravityProductCreateRequest(
                        title,
                        description ?? title,
                        brand_id,
                        brand_name,
                        category_id,
                        category_path,
                        resolvedImage),
                    cancellationToken);
                return GravityAgentContext.Json(new { id = created.ProductId, name = created.Name });
            },
            name: Name,
            description: AgentTools.Description(services, Name));
}

internal static class ProductListTool
{
    public const string Name = "product_list";

    public static AITool Create(IServiceProvider services, string tenantId)
        => AIFunctionFactory.Create(
            async (int? page, int? page_size, CancellationToken cancellationToken) =>
            {
                _ = tenantId;
                var (client, credentials) = await GravityAgentContext.ResolveAsync(services, cancellationToken);
                var result = await client.ListProductsAsync(credentials, page ?? 1, page_size ?? 20, cancellationToken);
                return GravityAgentContext.Json(result.Results.Select(x => new
                {
                    id = x.ProductId,
                    name = x.Name,
                    brand = x.BrandName,
                    category = x.CategoryPath,
                    status = x.ProductStatusName
                }));
            },
            name: Name,
            description: AgentTools.Description(services, Name));
}

internal static class ProductUpdateTool
{
    public const string Name = "product_update";

    public static AITool Create(IServiceProvider services, string tenantId)
        => AIFunctionFactory.Create(
            async (
                string product_id,
                string title,
                string? description,
                string? brand_id,
                string? brand_name,
                string? category_id,
                string? category_path,
                string? image_url,
                CancellationToken cancellationToken) =>
            {
                _ = tenantId;
                var (client, credentials) = await GravityAgentContext.ResolveAsync(services, cancellationToken);
                var updated = await client.UpdateProductAsync(
                    credentials,
                    product_id,
                    new GravityProductCreateRequest(
                        title,
                        description ?? title,
                        brand_id,
                        brand_name,
                        category_id,
                        category_path,
                        image_url),
                    cancellationToken);
                return GravityAgentContext.Json(new { id = updated.ProductId, name = updated.Name });
            },
            name: Name,
            description: AgentTools.Description(services, Name));
}

internal static class ProductDeleteTool
{
    public const string Name = "product_delete";

    public static AITool Create(IServiceProvider services, string tenantId)
        => AIFunctionFactory.Create(
            async (string product_id, CancellationToken cancellationToken) =>
            {
                _ = tenantId;
                var (client, credentials) = await GravityAgentContext.ResolveAsync(services, cancellationToken);
                await client.DeleteProductAsync(credentials, product_id, cancellationToken);
                return GravityAgentContext.Json(new { deleted = true, id = product_id });
            },
            name: Name,
            description: AgentTools.Description(services, Name));
}

internal static class ProductPublishTool
{
    public const string Name = "product_publish";

    public static AITool Create(IServiceProvider services, string tenantId)
        => AIFunctionFactory.Create(
            async (string product_id, CancellationToken cancellationToken) =>
            {
                _ = tenantId;
                var (client, credentials) = await GravityAgentContext.ResolveAsync(services, cancellationToken);
                await client.PublishProductAsync(credentials, product_id, cancellationToken);
                return GravityAgentContext.Json(new { published = true, id = product_id });
            },
            name: Name,
            description: AgentTools.Description(services, Name));
}
