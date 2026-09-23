using Microsoft.Extensions.AI;
using Microsoft.Extensions.DependencyInjection;
using Commerce.Application.Abstracts;
using Commerce.Infrastructure.Agents.Definitions.PostSalesSupport.Tools;

namespace Commerce.Infrastructure.Agents.Definitions.PostSalesSupport;

public sealed class PostSalesSupportAgent : IAgentModule
{
    public const string KeyName = "post-sales-support";

    public string Key => KeyName;

    public static IServiceCollection Add(IServiceCollection services)
        => services.AddScoped<IAgentModule, PostSalesSupportAgent>();

    public IReadOnlyList<AITool> CreateTools(AgentDefinition definition, string tenantId, IServiceProvider services)
    {
        var available = new Dictionary<string, AITool>(StringComparer.OrdinalIgnoreCase)
        {
            [GetOrderTimelineTool.Name] = GetOrderTimelineTool.Create(services),
            [CheckCarrierTool.Name] = CheckCarrierTool.Create(services),
            [CreateReturnTool.Name] = CreateReturnTool.Create(services),
            [SearchPoliciesTool.Name] = SearchPoliciesTool.Create(services, tenantId)
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
