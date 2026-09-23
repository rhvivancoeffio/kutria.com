using Microsoft.Extensions.AI;
using Microsoft.Extensions.DependencyInjection;
using Commerce.Application.Abstracts;

namespace Commerce.Infrastructure.Agents.Definitions.OperationsAdvisor.Tools;

internal static class GetKpiTool
{
    public const string Name = "get_kpi";

    public static AITool Create(IServiceProvider services)
    {
        var kpis = services.GetRequiredService<IKpiTools>();
        return AIFunctionFactory.Create(
            (string kpiName) => kpis.GetKpiAsync(kpiName),
            name: Name,
            description: AgentTools.Description(services, Name));
    }
}
