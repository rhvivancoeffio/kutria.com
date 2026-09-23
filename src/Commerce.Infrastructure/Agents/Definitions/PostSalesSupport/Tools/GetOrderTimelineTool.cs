using Microsoft.Extensions.AI;
using Microsoft.Extensions.DependencyInjection;
using Commerce.Application.Abstracts;

namespace Commerce.Infrastructure.Agents.Definitions.PostSalesSupport.Tools;

internal static class GetOrderTimelineTool
{
    public const string Name = "get_order_timeline";

    public static AITool Create(IServiceProvider services)
    {
        var orders = services.GetRequiredService<IOrderTools>();
        return AIFunctionFactory.Create(
            (string orderId) => orders.GetOrderTimelineAsync(orderId),
            name: Name,
            description: AgentTools.Description(services, Name));
    }
}
