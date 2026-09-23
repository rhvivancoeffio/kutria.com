using Microsoft.Extensions.AI;
using Microsoft.Extensions.DependencyInjection;
using Commerce.Application.Abstracts;
using Commerce.Infrastructure.Agents.GravityCatalog;

namespace Commerce.Infrastructure.Agents.Definitions.BrandManager;

public sealed class BrandManagerAgent : IAgentModule
{
    public const string KeyName = "brand-manager";

    public string Key => KeyName;

    public static IServiceCollection Add(IServiceCollection services)
        => services.AddScoped<IAgentModule, BrandManagerAgent>();

    public IReadOnlyList<AITool> CreateTools(AgentDefinition definition, string tenantId, IServiceProvider services)
    {
        var available = new Dictionary<string, AITool>(StringComparer.OrdinalIgnoreCase)
        {
            [BrandListTool.Name] = BrandListTool.Create(services, tenantId),
            [BrandCreateTool.Name] = BrandCreateTool.Create(services, tenantId),
            [BrandUpdateTool.Name] = BrandUpdateTool.Create(services, tenantId),
            [BrandDeleteTool.Name] = BrandDeleteTool.Create(services, tenantId)
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

internal static class BrandListTool
{
    public const string Name = "brand_list";

    public static AITool Create(IServiceProvider services, string tenantId)
        => AIFunctionFactory.Create(
            async (string? name, int? page, int? page_size, CancellationToken cancellationToken) =>
            {
                _ = tenantId;
                var (client, credentials) = await GravityAgentContext.ResolveAsync(services, cancellationToken);
                var items = await client.ListBrandsAsync(
                    credentials,
                    name,
                    page ?? 1,
                    page_size ?? 20,
                    cancellationToken);
                return GravityAgentContext.Json(items.Select(x => new { id = x.BrandId, name = x.Name, is_active = x.IsActive }));
            },
            name: Name,
            description: AgentTools.Description(services, Name));
}

internal static class BrandCreateTool
{
    public const string Name = "brand_create";

    public static AITool Create(IServiceProvider services, string tenantId)
        => AIFunctionFactory.Create(
            async (string name, string? description, string? image_url, CancellationToken cancellationToken) =>
            {
                _ = tenantId;
                var (client, credentials) = await GravityAgentContext.ResolveAsync(services, cancellationToken);
                var created = await client.CreateBrandAsync(
                    credentials,
                    new GravityBrandCreateRequest(name, description, ImageUrl: image_url),
                    cancellationToken);
                return GravityAgentContext.Json(new { id = created.BrandId, name = created.Name });
            },
            name: Name,
            description: AgentTools.Description(services, Name));
}

internal static class BrandUpdateTool
{
    public const string Name = "brand_update";

    public static AITool Create(IServiceProvider services, string tenantId)
        => AIFunctionFactory.Create(
            async (string brand_id, string name, string? description, string? image_url, CancellationToken cancellationToken) =>
            {
                _ = tenantId;
                var (client, credentials) = await GravityAgentContext.ResolveAsync(services, cancellationToken);
                var updated = await client.UpdateBrandAsync(
                    credentials,
                    brand_id,
                    new GravityBrandCreateRequest(name, description, ImageUrl: image_url),
                    cancellationToken);
                return GravityAgentContext.Json(new { id = updated.BrandId, name = updated.Name });
            },
            name: Name,
            description: AgentTools.Description(services, Name));
}

internal static class BrandDeleteTool
{
    public const string Name = "brand_delete";

    public static AITool Create(IServiceProvider services, string tenantId)
        => AIFunctionFactory.Create(
            async (string brand_id, CancellationToken cancellationToken) =>
            {
                _ = tenantId;
                var (client, credentials) = await GravityAgentContext.ResolveAsync(services, cancellationToken);
                await client.DeleteBrandAsync(credentials, brand_id, cancellationToken);
                return GravityAgentContext.Json(new { deleted = true, id = brand_id });
            },
            name: Name,
            description: AgentTools.Description(services, Name));
}
