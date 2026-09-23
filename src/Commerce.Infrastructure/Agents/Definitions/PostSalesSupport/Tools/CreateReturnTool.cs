using Microsoft.Extensions.AI;
using Microsoft.Extensions.DependencyInjection;
using Commerce.Application.Abstracts;

namespace Commerce.Infrastructure.Agents.Definitions.PostSalesSupport.Tools;

internal static class CreateReturnTool
{
    public const string Name = "create_return";

    public static AITool Create(IServiceProvider services)
    {
        var orders = services.GetRequiredService<IOrderTools>();
        return AIFunctionFactory.Create(
            (string orderId) => orders.CreateReturnAsync(orderId),
            name: Name,
            description: AgentTools.Description(services, Name));
    }
}
