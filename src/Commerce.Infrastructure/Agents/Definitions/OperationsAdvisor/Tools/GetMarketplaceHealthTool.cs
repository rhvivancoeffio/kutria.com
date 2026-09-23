using Microsoft.Extensions.AI;
using Microsoft.Extensions.DependencyInjection;
using Commerce.Application.Abstracts;

namespace Commerce.Infrastructure.Agents.Definitions.OperationsAdvisor.Tools;

internal static class GetMarketplaceHealthTool
{
    public const string Name = "get_marketplace_health";

    public static AITool Create(IServiceProvider services)
    {
        var kpis = services.GetRequiredService<IKpiTools>();
        return AIFunctionFactory.Create(
            () => kpis.GetMarketplaceHealthAsync(),
            name: Name,
            description: AgentTools.Description(services, Name));
    }
}
