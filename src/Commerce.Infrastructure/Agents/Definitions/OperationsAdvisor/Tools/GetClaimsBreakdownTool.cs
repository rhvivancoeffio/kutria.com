using Microsoft.Extensions.AI;
using Microsoft.Extensions.DependencyInjection;
using Commerce.Application.Abstracts;

namespace Commerce.Infrastructure.Agents.Definitions.OperationsAdvisor.Tools;

internal static class GetClaimsBreakdownTool
{
    public const string Name = "get_claims_breakdown";

    public static AITool Create(IServiceProvider services)
    {
        var kpis = services.GetRequiredService<IKpiTools>();
        return AIFunctionFactory.Create(
            () => kpis.GetClaimsBreakdownAsync(),
            name: Name,
            description: AgentTools.Description(services, Name));
    }
}
