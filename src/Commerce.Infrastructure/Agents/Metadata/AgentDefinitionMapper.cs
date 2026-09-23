using System.Security.Cryptography;
using System.Text;
using Commerce.Application.Abstracts;

namespace Commerce.Infrastructure.Agents.Metadata;

internal static class AgentDefinitionMapper
{
    public static string Hash(string yaml)
        => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(yaml)));

    public static AgentDefinition ToDefinition(
        AgentYamlDocument document,
        string yaml,
        bool isTenantOverride,
        string instructions,
        string outputSchema,
        string? hash = null)
    {
        return new AgentDefinition(
            document.Key ?? string.Empty,
            document.Kind ?? string.Empty,
            document.Name ?? string.Empty,
            instructions,
            document.Model?.Name,
            document.Queue,
            ToolNames(document),
            document.Publishes ?? [],
            yaml,
            hash ?? Hash(yaml),
            isTenantOverride,
            document.Model?.Temperature is double temperature ? (float)temperature : null,
            document.Model?.MaxTokens,
            outputSchema,
            document.Guardrails?.ForbiddenPhrases ?? [],
            document.Description ?? string.Empty,
            string.IsNullOrWhiteSpace(document.Audience) ? "buyer" : document.Audience,
            ToolRequires(document));
    }

    public static string ComposeInstructions(AgentYamlDocument document, string body)
    {
        var clauses = new List<string>();
        if (document.Guardrails?.NoInvent == true)
        {
            clauses.Add("No inventes precios, stock, tracking ni estado de pago.");
        }

        if (document.Guardrails?.ForbiddenPhrases is { Count: > 0 } phrases)
        {
            clauses.Add("Nunca uses: " + string.Join(", ", phrases) + ".");
        }

        if (string.Equals(document.Kind, "conversational", StringComparison.OrdinalIgnoreCase))
        {
            clauses.Add("La respuesta final es solo el JSON del schema. message es el texto que ve el cliente.");
        }

        foreach (var tool in document.Tools ?? [])
        {
            if (string.IsNullOrWhiteSpace(tool.Name) || tool.Requires is not { Count: > 0 } required)
            {
                continue;
            }

            clauses.Add($"Antes de {tool.Name}, llama {string.Join(" y ", required)}.");
        }

        var text = body.Trim();
        if (clauses.Count == 0)
        {
            return text;
        }

        var builder = new StringBuilder(text);
        if (builder.Length > 0)
        {
            builder.AppendLine().AppendLine();
        }

        builder.AppendLine("Contrato:");
        foreach (var clause in clauses)
        {
            builder.Append("- ").AppendLine(clause);
        }

        return builder.ToString().TrimEnd();
    }

    public static void EnsureTenantOverlayIsAllowed(AgentYamlDocument system, AgentYamlDocument tenant, string agentKey)
    {
        if (!string.Equals(tenant.Key, agentKey, StringComparison.OrdinalIgnoreCase)
            || !string.Equals(tenant.Key, system.Key, StringComparison.OrdinalIgnoreCase))
        {
            throw new AgentDefinitionRejectedException($"Agent key must stay '{system.Key}'.");
        }

        if (!string.Equals(tenant.Kind, system.Kind, StringComparison.OrdinalIgnoreCase))
        {
            throw new AgentDefinitionRejectedException("kind cannot be changed.");
        }

        if (!string.Equals(tenant.Name, system.Name, StringComparison.Ordinal))
        {
            throw new AgentDefinitionRejectedException("name cannot be changed. Only instructions and model may change.");
        }

        if (tenant.Version is not null && !string.Equals(tenant.Version, system.Version, StringComparison.Ordinal))
        {
            throw new AgentDefinitionRejectedException("version cannot be changed.");
        }

        if (tenant.Description is not null && !string.Equals(tenant.Description, system.Description, StringComparison.Ordinal))
        {
            throw new AgentDefinitionRejectedException("description cannot be changed.");
        }

        if (tenant.Description is not null && !string.Equals(tenant.Description, system.Description, StringComparison.Ordinal))
        {
            throw new AgentDefinitionRejectedException("description cannot be changed. It is used for routing.");
        }

        if (tenant.Audience is not null && !string.Equals(tenant.Audience, system.Audience, StringComparison.OrdinalIgnoreCase))
        {
            throw new AgentDefinitionRejectedException("audience cannot be changed.");
        }

        if (tenant.Prompt?.SystemFile is not null
            && !string.Equals(tenant.Prompt.SystemFile, system.Prompt?.SystemFile, StringComparison.Ordinal))
        {
            throw new AgentDefinitionRejectedException("prompt.system_file cannot be changed. Override instructions instead.");
        }

        if (!string.Equals(tenant.Output?.SchemaFile, system.Output?.SchemaFile, StringComparison.Ordinal))
        {
            throw new AgentDefinitionRejectedException("output.schema_file cannot be changed.");
        }

        if (!string.Equals(tenant.Queue, system.Queue, StringComparison.Ordinal))
        {
            throw new AgentDefinitionRejectedException("queue cannot be changed.");
        }

        var systemPublishes = system.Publishes ?? [];
        var tenantPublishes = tenant.Publishes ?? [];
        if (!systemPublishes.SequenceEqual(tenantPublishes, StringComparer.Ordinal))
        {
            throw new AgentDefinitionRejectedException("publishes cannot be changed.");
        }

        EnsureGuardrailsMatch(system.Guardrails, tenant.Guardrails);
        EnsureToolsMatch(system.Tools ?? [], tenant.Tools ?? []);
    }

    private static void EnsureGuardrailsMatch(AgentYamlGuardrails? system, AgentYamlGuardrails? tenant)
    {
        if (system is null)
        {
            return;
        }

        tenant ??= new AgentYamlGuardrails();
        if (tenant.NoInvent != system.NoInvent)
        {
            throw new AgentDefinitionRejectedException("guardrails.no_invent cannot be changed.");
        }

        if (!Same(tenant.ForbiddenPhrases ?? [], system.ForbiddenPhrases ?? []))
        {
            throw new AgentDefinitionRejectedException("guardrails.forbidden_phrases cannot be changed.");
        }
    }

    private static void EnsureToolsMatch(IReadOnlyList<AgentYamlTool> system, IReadOnlyList<AgentYamlTool> tenant)
    {
        var systemByName = system
            .Where(tool => !string.IsNullOrWhiteSpace(tool.Name))
            .ToDictionary(tool => tool.Name!, StringComparer.OrdinalIgnoreCase);

        foreach (var tool in tenant)
        {
            if (string.IsNullOrWhiteSpace(tool.Name) || !systemByName.TryGetValue(tool.Name, out var allowed))
            {
                throw new AgentDefinitionRejectedException($"Unknown tool '{tool.Name}'. Tools must stay a subset of the system definition.");
            }

            if (tool.TimeoutMs is int timeout && timeout != allowed.TimeoutMs)
            {
                throw new AgentDefinitionRejectedException($"timeout_ms for '{tool.Name}' cannot be changed.");
            }

            if (tool.SideEffect is bool sideEffect && sideEffect != (allowed.SideEffect ?? false))
            {
                throw new AgentDefinitionRejectedException($"side_effect for '{tool.Name}' cannot be changed.");
            }

            if (tool.Requires is not null && !Same(tool.Requires, allowed.Requires ?? []))
            {
                throw new AgentDefinitionRejectedException($"requires for '{tool.Name}' cannot be changed.");
            }
        }
    }

    public static Dictionary<string, IReadOnlyList<string>> ToolRequires(AgentYamlDocument document)
        => (document.Tools ?? [])
            .Where(tool => !string.IsNullOrWhiteSpace(tool.Name) && tool.Requires is { Count: > 0 })
            .ToDictionary(
                tool => tool.Name!,
                tool => (IReadOnlyList<string>)tool.Requires!,
                StringComparer.OrdinalIgnoreCase);

    public static List<string> ToolNames(AgentYamlDocument document)
        => (document.Tools ?? [])
            .Select(tool => tool.Name)
            .Where(name => !string.IsNullOrWhiteSpace(name))
            .Select(name => name!)
            .ToList();

    private static bool Same(IReadOnlyList<string> left, IReadOnlyList<string> right)
        => left.SequenceEqual(right, StringComparer.Ordinal);
}
