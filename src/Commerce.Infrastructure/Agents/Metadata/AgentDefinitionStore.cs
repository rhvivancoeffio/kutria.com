using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Commerce.Application.Abstracts;
using Commerce.Domain.Agents;

namespace Commerce.Infrastructure.Agents.Metadata;

public sealed class AgentDefinitionStore(
    IYamlMetadataService yaml,
    ICommerceDbContext db) : IAgentDefinitionStore
{
    private readonly object _systemGate = new();
    private IReadOnlyList<AgentDefinition>? _systemCache;

    public async Task<IReadOnlyList<AgentDefinition>> ListAsync(string tenantId, CancellationToken cancellationToken = default)
    {
        _ = tenantId;
        // Tenant YAML overrides disabled for now — system catalog only.
        var system = await LoadSystemAsync(cancellationToken);
        return system.Select(WithoutYaml).ToList();
    }

    public async Task<AgentDefinition?> GetAsync(string tenantId, string agentKey, CancellationToken cancellationToken = default)
    {
        _ = tenantId;
        return await FindSystemAsync(agentKey, cancellationToken);
    }

    public async Task<IReadOnlyList<AgentDefinition>> ListSystemAsync(CancellationToken cancellationToken = default)
        => await LoadSystemAsync(cancellationToken);

    public async Task<AgentDefinition> SaveTenantYamlAsync(
        string tenantId,
        string agentKey,
        string yamlDocument,
        CancellationToken cancellationToken = default)
    {
        var system = await FindSystemAsync(agentKey, cancellationToken)
            ?? throw new AgentDefinitionRejectedException($"Unknown agent '{agentKey}'.");

        var systemDocument = yaml.DeserializeAgentYamlDocument<AgentYamlDocument>(system.Yaml);
        var tenantDocument = yaml.DeserializeAgentYamlDocument<AgentYamlDocument>(yamlDocument);
        AgentDefinitionMapper.EnsureTenantOverlayIsAllowed(systemDocument, tenantDocument, agentKey);

        var existing = await db.TenantAgentDefinitions
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(x => x.TenantId == tenantId && x.AgentKey == agentKey, cancellationToken);

        if (existing is null)
        {
            existing = new TenantAgentDefinition
            {
                TenantId = tenantId,
                AgentKey = agentKey
            };
            db.TenantAgentDefinitions.Add(existing);
        }

        var instructions = await ResolveInstructionsAsync(tenantDocument, AgentDirectory(agentKey), cancellationToken);
        existing.Kind = tenantDocument.Kind ?? string.Empty;
        existing.Name = tenantDocument.Name ?? string.Empty;
        existing.Instructions = instructions;
        existing.Model = tenantDocument.Model?.Name;
        existing.Queue = tenantDocument.Queue;
        existing.Tools = AgentDefinitionMapper.ToolNames(tenantDocument);
        existing.Publishes = tenantDocument.Publishes ?? [];
        existing.Yaml = yamlDocument;
        existing.BasedOnHash = system.Hash;
        existing.UpdatedAt = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync(cancellationToken);

        // Runtime still serves system YAML until overrides are re-enabled.
        return WithSystemContract(
            AgentDefinitionMapper.ToDefinition(tenantDocument, yamlDocument, isTenantOverride: true, instructions, system.OutputSchema, hash: system.Hash),
            system);
    }

    public async Task ResetTenantAsync(string tenantId, string agentKey, CancellationToken cancellationToken = default)
    {
        await db.TenantAgentDefinitions
            .IgnoreQueryFilters()
            .Where(x => x.TenantId == tenantId && x.AgentKey == agentKey)
            .ExecuteDeleteAsync(cancellationToken);
    }

    private async Task<AgentDefinition?> FindSystemAsync(string agentKey, CancellationToken cancellationToken)
    {
        var all = await LoadSystemAsync(cancellationToken);
        return all.FirstOrDefault(x => string.Equals(x.Key, agentKey, StringComparison.OrdinalIgnoreCase));
    }

    private async Task<IReadOnlyList<AgentDefinition>> LoadSystemAsync(CancellationToken cancellationToken)
    {
        lock (_systemGate)
        {
            if (_systemCache is not null)
                return _systemCache;
        }

        var agentsDir = Path.Combine(yaml.GetDataRootPath(), "agents");
        if (!Directory.Exists(agentsDir))
        {
            return [];
        }

        var files = Directory.GetFiles(agentsDir, "*.yaml", SearchOption.AllDirectories)
            .OrderBy(path => path, StringComparer.OrdinalIgnoreCase);

        var result = new List<AgentDefinition>();
        foreach (var file in files)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var content = await File.ReadAllTextAsync(file, cancellationToken);
            var document = yaml.DeserializeAgentYamlDocument<AgentYamlDocument>(content);
            if (string.IsNullOrWhiteSpace(document.Key))
            {
                continue;
            }

            var directory = Path.GetDirectoryName(file) ?? agentsDir;
            var folder = Path.GetFileName(directory);
            if (!string.Equals(folder, document.Key, StringComparison.OrdinalIgnoreCase)
                || !string.Equals(Path.GetFileName(file), "agent.yaml", StringComparison.OrdinalIgnoreCase))
            {
                throw new AgentDefinitionRejectedException(
                    $"Agent '{document.Key}' must live at data/agents/{document.Key}/agent.yaml.");
            }

            if (result.Any(agent => string.Equals(agent.Key, document.Key, StringComparison.OrdinalIgnoreCase)))
            {
                throw new AgentDefinitionRejectedException($"Duplicate agent key '{document.Key}'.");
            }

            var schema = await LoadOutputSchemaAsync(document, directory, cancellationToken);
            var instructions = await ResolveInstructionsAsync(document, directory, cancellationToken);
            result.Add(AgentDefinitionMapper.ToDefinition(document, content, isTenantOverride: false, instructions, schema));
        }

        lock (_systemGate)
        {
            _systemCache ??= result;
            return _systemCache;
        }
    }

    private static AgentDefinition WithSystemContract(AgentDefinition tenant, AgentDefinition system)
        => tenant with
        {
            OutputSchema = system.OutputSchema,
            ForbiddenPhrases = system.ForbiddenPhrases,
            Description = system.Description,
            Audience = system.Audience,
            ToolRequires = system.ToolRequires
        };

    private static async Task<string> LoadOutputSchemaAsync(
        AgentYamlDocument document,
        string agentDirectory,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(document.Output?.SchemaFile))
        {
            throw new AgentDefinitionRejectedException($"Agent '{document.Key}' must declare output.schema_file.");
        }

        var path = ResolvePromptPath(agentDirectory, document.Output.SchemaFile);
        var schema = await File.ReadAllTextAsync(path, cancellationToken);
        using var parsed = JsonDocument.Parse(schema);
        if (parsed.RootElement.ValueKind != JsonValueKind.Object)
        {
            throw new AgentDefinitionRejectedException($"Output schema for '{document.Key}' must be a JSON object.");
        }

        if (string.Equals(document.Kind, "conversational", StringComparison.OrdinalIgnoreCase)
            && !HasStringProperty(parsed.RootElement, "message"))
        {
            throw new AgentDefinitionRejectedException($"Conversational agent '{document.Key}' output must include a string message.");
        }

        return schema;
    }

    private static bool HasStringProperty(JsonElement schema, string name)
    {
        if (!schema.TryGetProperty("properties", out var properties)
            || !properties.TryGetProperty(name, out var property))
        {
            return false;
        }

        if (!property.TryGetProperty("type", out var type))
        {
            return false;
        }

        return type.ValueKind == JsonValueKind.String
            ? string.Equals(type.GetString(), "string", StringComparison.OrdinalIgnoreCase)
            : type.ValueKind == JsonValueKind.Array
                && type.EnumerateArray().Any(item => string.Equals(item.GetString(), "string", StringComparison.OrdinalIgnoreCase));
    }

    private string AgentDirectory(string agentKey)
    {
        if (string.IsNullOrWhiteSpace(agentKey)
            || agentKey.Contains("..", StringComparison.Ordinal)
            || agentKey.Contains('/')
            || agentKey.Contains('\\'))
        {
            throw new AgentDefinitionRejectedException("Invalid agent key.");
        }

        return Path.GetFullPath(Path.Combine(yaml.GetDataRootPath(), "agents", agentKey));
    }

    private static async Task<string> ResolveInstructionsAsync(
        AgentYamlDocument document,
        string agentDirectory,
        CancellationToken cancellationToken)
    {
        if (!string.IsNullOrWhiteSpace(document.Instructions))
        {
            return AgentDefinitionMapper.ComposeInstructions(document, document.Instructions);
        }

        if (string.IsNullOrWhiteSpace(document.Prompt?.SystemFile))
        {
            return AgentDefinitionMapper.ComposeInstructions(document, string.Empty);
        }

        var path = ResolvePromptPath(agentDirectory, document.Prompt.SystemFile);
        var body = await File.ReadAllTextAsync(path, cancellationToken);
        return AgentDefinitionMapper.ComposeInstructions(document, body);
    }

    private static string ResolvePromptPath(string agentDirectory, string systemFile)
    {
        var relative = systemFile.Replace('\\', '/').Trim();
        while (relative.StartsWith("./", StringComparison.Ordinal))
        {
            relative = relative[2..];
        }

        relative = relative.TrimStart('/');
        if (relative.Contains("..", StringComparison.Ordinal) || Path.IsPathRooted(systemFile))
        {
            throw new AgentDefinitionRejectedException("prompt.system_file must stay inside the agent folder.");
        }

        var root = Path.GetFullPath(agentDirectory);
        var full = Path.GetFullPath(Path.Combine(root, relative));
        var prefix = root.EndsWith(Path.DirectorySeparatorChar) ? root : root + Path.DirectorySeparatorChar;
        if (!full.StartsWith(prefix, StringComparison.Ordinal) || !File.Exists(full))
        {
            throw new AgentDefinitionRejectedException($"Prompt file '{systemFile}' was not found in the agent folder.");
        }

        return full;
    }

    private static AgentDefinition WithoutYaml(AgentDefinition definition)
        => definition with { Yaml = string.Empty };
}
