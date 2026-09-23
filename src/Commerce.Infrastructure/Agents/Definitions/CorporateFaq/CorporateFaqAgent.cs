using Microsoft.Extensions.AI;
using Microsoft.Extensions.DependencyInjection;
using Commerce.Application.Abstracts;
using Commerce.Infrastructure.Agents.Definitions.PostSalesSupport.Tools;

namespace Commerce.Infrastructure.Agents.Definitions.CorporateFaq;

public sealed class CorporateFaqAgent : IAgentModule
{
    public const string KeyName = "corporate-faq";

    public string Key => KeyName;

    public static IServiceCollection Add(IServiceCollection services)
        => services.AddScoped<IAgentModule, CorporateFaqAgent>();

    public IReadOnlyList<AITool> CreateTools(AgentDefinition definition, string tenantId, IServiceProvider services)
    {
        var available = new Dictionary<string, AITool>(StringComparer.OrdinalIgnoreCase)
        {
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
