using Microsoft.Extensions.AI;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Commerce.Application.Abstracts;
using Commerce.Infrastructure.Agents.Metadata;

namespace Commerce.Infrastructure.Agents;

internal static class AgentTools
{
    /// <summary>LLM-facing tool description from <c>data/agents/tools/{name}.yaml</c>.</summary>
    public static string Description(IServiceProvider services, string name)
        => services.GetRequiredService<IToolCatalog>().ResolveDescription(name, name);

    public static IReadOnlyList<AITool> Bind(
        AgentDefinition definition,
        IReadOnlyDictionary<string, AITool> available,
        IServiceProvider services)
    {
        var catalog = services.GetRequiredService<IToolCatalog>();
        var logger = services.GetRequiredService<ILoggerFactory>().CreateLogger("AgentTools");
        var turn = ToolCallTurn.Current.Value;
        var requires = definition.ToolRequires
            ?? new Dictionary<string, IReadOnlyList<string>>(StringComparer.OrdinalIgnoreCase);

        return definition.Tools
            .Where(available.ContainsKey)
            .Select(name =>
            {
                var tool = available[name];
                if (tool is AIFunction function)
                {
                    // Always pin the catalog description so the model never sees the Name placeholder.
                    var description = catalog.ResolveDescription(name, function.Description);
                    function = new YamlDescribedAIFunction(function, description);
                    function = new ExceptionLoggingAIFunction(function, logger);
                    return Wrap(function, name, requires, turn);
                }

                return tool;
            })
            .ToList();
    }

    private static AITool Wrap(
        AIFunction function,
        string name,
        IReadOnlyDictionary<string, IReadOnlyList<string>> requires,
        ToolCallTurn? turn)
    {
        if (turn is null)
            return function;

        if (!requires.TryGetValue(name, out var required) || required.Count == 0)
            return new RequiredPredecessorTool(function, [], turn);

        return new RequiredPredecessorTool(function, required, turn);
    }
}
