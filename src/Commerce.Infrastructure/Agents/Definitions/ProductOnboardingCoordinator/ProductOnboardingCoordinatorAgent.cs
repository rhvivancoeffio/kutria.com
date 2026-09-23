using Microsoft.Extensions.AI;
using Microsoft.Extensions.DependencyInjection;
using Commerce.Application.Abstracts;
using Commerce.Infrastructure.Agents.Definitions.BrandManager;
using Commerce.Infrastructure.Agents.Definitions.CategoryManager;
using Commerce.Infrastructure.Agents.Definitions.ProductManager;

namespace Commerce.Infrastructure.Agents.Definitions.ProductOnboardingCoordinator;

/// <summary>
/// Optional coordinator module. Tools are owned by Brand/Category/Product managers; this only composes them.
/// </summary>
public sealed class ProductOnboardingCoordinatorAgent : IAgentModule
{
    public const string KeyName = "product-onboarding-coordinator";

    public string Key => KeyName;

    public static IServiceCollection Add(IServiceCollection services)
        => services.AddScoped<IAgentModule, ProductOnboardingCoordinatorAgent>();

    public IReadOnlyList<AITool> CreateTools(AgentDefinition definition, string tenantId, IServiceProvider services)
    {
        var available = new Dictionary<string, AITool>(StringComparer.OrdinalIgnoreCase)
        {
            [ImageAnalyzeTool.Name] = ImageAnalyzeTool.Create(services, tenantId),
            [BrandListTool.Name] = BrandListTool.Create(services, tenantId),
            [BrandCreateTool.Name] = BrandCreateTool.Create(services, tenantId),
            [CategoryListTool.Name] = CategoryListTool.Create(services, tenantId),
            [CategoryCreateTool.Name] = CategoryCreateTool.Create(services, tenantId),
            [ProductCreateTool.Name] = ProductCreateTool.Create(services, tenantId)
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
