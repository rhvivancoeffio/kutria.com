using Microsoft.Extensions.AI;
using Microsoft.Extensions.DependencyInjection;
using Commerce.Application.Abstracts;
using Commerce.Infrastructure.Agents.GravityCatalog;

namespace Commerce.Infrastructure.Agents.Definitions.CategoryManager;

public sealed class CategoryManagerAgent : IAgentModule
{
    public const string KeyName = "category-manager";

    public string Key => KeyName;

    public static IServiceCollection Add(IServiceCollection services)
        => services.AddScoped<IAgentModule, CategoryManagerAgent>();

    public IReadOnlyList<AITool> CreateTools(AgentDefinition definition, string tenantId, IServiceProvider services)
    {
        var available = new Dictionary<string, AITool>(StringComparer.OrdinalIgnoreCase)
        {
            [CategoryListTool.Name] = CategoryListTool.Create(services, tenantId),
            [CategoryCreateTool.Name] = CategoryCreateTool.Create(services, tenantId),
            [CategoryUpdateTool.Name] = CategoryUpdateTool.Create(services, tenantId),
            [CategoryDeleteTool.Name] = CategoryDeleteTool.Create(services, tenantId)
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

internal static class CategoryListTool
{
    public const string Name = "category_list";

    public static AITool Create(IServiceProvider services, string tenantId)
        => AIFunctionFactory.Create(
            async (string? name, CancellationToken cancellationToken) =>
            {
                _ = tenantId;
                var (client, credentials) = await GravityAgentContext.ResolveAsync(services, cancellationToken);
                var items = await client.ListCategoriesAsync(credentials, name, cancellationToken);
                return GravityAgentContext.Json(items.Select(x => new { id = x.CategoryId, name = x.Name, parent_id = x.ParentId }));
            },
            name: Name,
            description: AgentTools.Description(services, Name));
}

internal static class CategoryCreateTool
{
    public const string Name = "category_create";

    public static AITool Create(IServiceProvider services, string tenantId)
        => AIFunctionFactory.Create(
            async (string name, string? slug, string? description, string? parent_category_id, CancellationToken cancellationToken) =>
            {
                _ = tenantId;
                var (client, credentials) = await GravityAgentContext.ResolveAsync(services, cancellationToken);
                var created = await client.CreateCategoryAsync(
                    credentials,
                    new GravityCategoryCreateRequest(name, slug, description, parent_category_id),
                    cancellationToken);
                return GravityAgentContext.Json(new { id = created.CategoryId, name = created.Name });
            },
            name: Name,
            description: AgentTools.Description(services, Name));
}

internal static class CategoryUpdateTool
{
    public const string Name = "category_update";

    public static AITool Create(IServiceProvider services, string tenantId)
        => AIFunctionFactory.Create(
            async (string category_id, string name, string? slug, string? description, string? parent_category_id, CancellationToken cancellationToken) =>
            {
                _ = tenantId;
                var (client, credentials) = await GravityAgentContext.ResolveAsync(services, cancellationToken);
                var updated = await client.UpdateCategoryAsync(
                    credentials,
                    category_id,
                    new GravityCategoryCreateRequest(name, slug, description, parent_category_id),
                    cancellationToken);
                return GravityAgentContext.Json(new { id = updated.CategoryId, name = updated.Name });
            },
            name: Name,
            description: AgentTools.Description(services, Name));
}

internal static class CategoryDeleteTool
{
    public const string Name = "category_delete";

    public static AITool Create(IServiceProvider services, string tenantId)
        => AIFunctionFactory.Create(
            async (string category_id, CancellationToken cancellationToken) =>
            {
                _ = tenantId;
                var (client, credentials) = await GravityAgentContext.ResolveAsync(services, cancellationToken);
                await client.DeleteCategoryAsync(credentials, category_id, cancellationToken);
                return GravityAgentContext.Json(new { deleted = true, id = category_id });
            },
            name: Name,
            description: AgentTools.Description(services, Name));
}
