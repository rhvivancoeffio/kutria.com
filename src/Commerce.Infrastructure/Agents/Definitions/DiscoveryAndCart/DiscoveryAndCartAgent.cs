using Microsoft.Extensions.AI;
using Microsoft.Extensions.DependencyInjection;
using Commerce.Application.Abstracts;
using Commerce.Infrastructure.Agents.Definitions.DiscoveryAndCart.Tools;

namespace Commerce.Infrastructure.Agents.Definitions.DiscoveryAndCart;

/// <summary>
/// Conversational agent. An agent folder holds the module plus a Tools folder for each side effect it may call.
/// </summary>
public sealed class DiscoveryAndCartAgent : IAgentModule
{
    public const string KeyName = "discovery-cart";

    public string Key => KeyName;

    public static IServiceCollection Add(IServiceCollection services)
        => services.AddScoped<IAgentModule, DiscoveryAndCartAgent>();

    public IReadOnlyList<AITool> CreateTools(AgentDefinition definition, string tenantId, IServiceProvider services)
    {
        // Capture once here — MAF tool Invoke does not flow AsyncLocal ChatTurnScope.
        var session = CartToolSupport.TryCaptureSession();

        var available = new Dictionary<string, AITool>(StringComparer.OrdinalIgnoreCase)
        {
            [SearchProductsTool.Name] = SearchProductsTool.Create(services, tenantId),
            [CompareProductsTool.Name] = CompareProductsTool.Create(services, tenantId),
            [GetProductDetailsTool.Name] = GetProductDetailsTool.Create(services, tenantId),
            [CreateCartTool.Name] = CreateCartTool.Create(services, session),
            [GetCartTool.Name] = GetCartTool.Create(services, session),
            [AddItemTool.Name] = AddItemTool.Create(services, session),
            [UpdateItemQtyTool.Name] = UpdateItemQtyTool.Create(services, session),
            [ApplyCouponTool.Name] = ApplyCouponTool.Create(services, session),
            [RemoveCouponTool.Name] = RemoveCouponTool.Create(services, session),
            [RemoveItemTool.Name] = RemoveItemTool.Create(services, session)
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
