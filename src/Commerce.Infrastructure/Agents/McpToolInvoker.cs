using System.Text.Json;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.DependencyInjection;
using Commerce.Application.Abstracts;

namespace Commerce.Infrastructure.Agents;

/// <summary>
/// Builds conversational AIFunctions for MCP CallTool, scoped by tenant and filtered by tool catalog <c>mcp: true</c>.
/// </summary>
public interface IMcpToolInvoker
{
    Task<IReadOnlyList<AIFunction>> CreateMcpFunctionsAsync(
        string tenantId,
        IServiceProvider services,
        CancellationToken cancellationToken = default);

    Task<string> CallAsync(
        string toolName,
        IDictionary<string, JsonElement>? arguments,
        string tenantId,
        IServiceProvider services,
        CancellationToken cancellationToken = default);
}

public sealed class McpToolInvoker(
    IEnumerable<IAgentModule> modules,
    IAgentDefinitionStore definitions,
    IToolCatalog catalog) : IMcpToolInvoker
{
    public async Task<IReadOnlyList<AIFunction>> CreateMcpFunctionsAsync(
        string tenantId,
        IServiceProvider services,
        CancellationToken cancellationToken = default)
    {
        var mcpNames = (await catalog.GetMcpToolsAsync(cancellationToken))
            .Select(t => t.Name)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var byName = new Dictionary<string, AIFunction>(StringComparer.OrdinalIgnoreCase);
        foreach (var module in modules)
        {
            var definition = await definitions.GetAsync(tenantId, module.Key, cancellationToken);
            if (definition is null)
                continue;

            foreach (var tool in module.CreateTools(definition, tenantId, services))
            {
                if (tool is not AIFunction fn || string.IsNullOrEmpty(fn.Name) || !mcpNames.Contains(fn.Name))
                    continue;
                byName[fn.Name] = fn;
            }
        }

        return byName.Values.ToList();
    }

    public async Task<string> CallAsync(
        string toolName,
        IDictionary<string, JsonElement>? arguments,
        string tenantId,
        IServiceProvider services,
        CancellationToken cancellationToken = default)
    {
        var functions = await CreateMcpFunctionsAsync(tenantId, services, cancellationToken);
        var fn = functions.FirstOrDefault(f => string.Equals(f.Name, toolName, StringComparison.OrdinalIgnoreCase))
            ?? throw new InvalidOperationException($"Unknown or non-MCP tool: '{toolName}'");

        AIFunctionArguments args;
        if (arguments is { Count: > 0 })
        {
            var dict = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
            foreach (var (key, value) in arguments)
            {
                dict[key] = ConvertJson(value);
            }
            args = new AIFunctionArguments(dict);
        }
        else
        {
            args = new AIFunctionArguments();
        }

        var result = await fn.InvokeAsync(args, cancellationToken: cancellationToken);
        return result switch
        {
            null => string.Empty,
            string s => s,
            JsonElement je => je.ToString(),
            _ => JsonSerializer.Serialize(result)
        };
    }

    private static object? ConvertJson(JsonElement el) => el.ValueKind switch
    {
        JsonValueKind.Null or JsonValueKind.Undefined => null,
        JsonValueKind.String => el.GetString(),
        JsonValueKind.Number when el.TryGetInt64(out var l) => l,
        JsonValueKind.Number when el.TryGetDecimal(out var d) => d,
        JsonValueKind.True => true,
        JsonValueKind.False => false,
        JsonValueKind.Array => el.EnumerateArray().Select(ConvertJson).ToArray(),
        JsonValueKind.Object => el,
        _ => el.ToString()
    };
}
