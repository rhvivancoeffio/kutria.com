using Microsoft.Extensions.AI;
using Microsoft.Extensions.DependencyInjection;
using Commerce.Application.Abstracts;
using Commerce.Infrastructure.Agents.Definitions.OperationsAdvisor.Tools;

namespace Commerce.Infrastructure.Agents.Definitions.OperationsAdvisor;

public sealed class OperationsAdvisorAgent : IAgentModule
{
    public const string KeyName = "operations-advisor";

    public string Key => KeyName;

    public static IServiceCollection Add(IServiceCollection services)
        => services.AddScoped<IAgentModule, OperationsAdvisorAgent>();

    public IReadOnlyList<AITool> CreateTools(AgentDefinition definition, string tenantId, IServiceProvider services)
    {
        var available = new Dictionary<string, AITool>(StringComparer.OrdinalIgnoreCase)
        {
            [GetKpiTool.Name] = GetKpiTool.Create(services),
            [GetOrdersDelayedTool.Name] = GetOrdersDelayedTool.Create(services),
            [GetClaimsBreakdownTool.Name] = GetClaimsBreakdownTool.Create(services),
            [GetMarketplaceHealthTool.Name] = GetMarketplaceHealthTool.Create(services),
            [ProposeFixTool.Name] = ProposeFixTool.Create(services, tenantId, definition.Key)
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
