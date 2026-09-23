using Microsoft.Extensions.AI;
using Microsoft.Extensions.DependencyInjection;
using Commerce.Application.Abstracts;

namespace Commerce.Infrastructure.Agents.Definitions.OperationsAdvisor.Tools;

internal static class GetOrdersDelayedTool
{
    public const string Name = "get_orders_delayed";

    public static AITool Create(IServiceProvider services)
    {
        var kpis = services.GetRequiredService<IKpiTools>();
        return AIFunctionFactory.Create(
            () => kpis.GetOrdersDelayedAsync(),
            name: Name,
            description: AgentTools.Description(services, Name));
    }
}
