using Microsoft.Extensions.AI;
using Microsoft.Extensions.DependencyInjection;
using Commerce.Application.Abstracts;
using Commerce.Infrastructure.Agents.Definitions.Checkout.Tools;
using Commerce.Infrastructure.Agents.Definitions.DiscoveryAndCart.Tools;

namespace Commerce.Infrastructure.Agents.Definitions.Checkout;

public sealed class CheckoutAgent : IAgentModule
{
    public const string KeyName = "checkout";

    public string Key => KeyName;

    public static IServiceCollection Add(IServiceCollection services)
        => services.AddScoped<IAgentModule, CheckoutAgent>();

    public IReadOnlyList<AITool> CreateTools(AgentDefinition definition, string tenantId, IServiceProvider services)
    {
        var session = CartToolSupport.TryCaptureSession();
        var available = new Dictionary<string, AITool>(StringComparer.OrdinalIgnoreCase)
        {
            [GetCartTool.Name] = GetCartTool.Create(services, session),
            [CartCheckoutTool.Name] = CartCheckoutTool.Create(services, session),
            [CreateOrderDraftTool.Name] = CreateOrderDraftTool.Create(services, session),
            [ApplyCouponTool.Name] = ApplyCouponTool.Create(services, session),
            [RemoveCouponTool.Name] = RemoveCouponTool.Create(services, session)
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
