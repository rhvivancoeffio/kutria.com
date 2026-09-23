using System.Collections.Concurrent;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Commerce.Application.Abstracts;

namespace Commerce.Infrastructure.Agents.Metadata;

/// <summary>
/// Loads <c>data/agents/tools/*.yaml</c> via <see cref="IYamlMetadataService"/> (agent underscored profile).
/// </summary>
public sealed class ToolCatalogService(
    IYamlMetadataService yaml,
    ILogger<ToolCatalogService> logger) : IToolCatalog
{
    private readonly ConcurrentDictionary<string, IReadOnlyList<ToolCatalogEntry>> _cache = new(StringComparer.OrdinalIgnoreCase);
    private static readonly JsonElement EmptyObjectSchema =
        JsonSerializer.Deserialize<JsonElement>("""{"type":"object","properties":{}}""");

    public async Task<IReadOnlyList<ToolCatalogEntry>> GetAllAsync(CancellationToken cancellationToken = default)
        => await LoadAsync(cancellationToken);

    public async Task<IReadOnlyList<ToolCatalogEntry>> GetMcpToolsAsync(CancellationToken cancellationToken = default)
    {
        var all = await LoadAsync(cancellationToken);
        return all.Where(t => t.Mcp).ToList();
    }

    public async Task<ToolCatalogEntry?> GetByNameAsync(string name, CancellationToken cancellationToken = default)
    {
        var all = await LoadAsync(cancellationToken);
        return all.FirstOrDefault(t => string.Equals(t.Name, name, StringComparison.OrdinalIgnoreCase));
    }

    public string ResolveDescription(string name, string? fallback = null)
    {
        if (string.IsNullOrWhiteSpace(name))
            return fallback ?? string.Empty;

        // First call may load YAML from disk; subsequent calls hit the cache.
        var entry = GetByNameAsync(name).GetAwaiter().GetResult();
        if (entry is not null && !string.IsNullOrWhiteSpace(entry.Description))
            return entry.Description;

        if (!string.IsNullOrWhiteSpace(fallback))
        {
            logger.LogWarning(
                "Tool YAML description missing for {ToolName}; using fallback.",
                name);
            return fallback;
        }

        logger.LogWarning("Tool YAML description missing for {ToolName}; using tool name.", name);
        return name;
    }

    private async Task<IReadOnlyList<ToolCatalogEntry>> LoadAsync(CancellationToken cancellationToken)
    {
        if (_cache.TryGetValue("all", out var cached))
        {
            return cached;
        }

        var root = Path.Combine(yaml.GetDataRootPath(), "agents", "tools");
        if (!Directory.Exists(root))
        {
            logger.LogWarning("Tool catalog directory missing: {Path}", root);
            return Array.Empty<ToolCatalogEntry>();
        }

        var entries = new List<ToolCatalogEntry>();
        foreach (var file in Directory.EnumerateFiles(root, "*.yaml", SearchOption.TopDirectoryOnly)
                     .Concat(Directory.EnumerateFiles(root, "*.yml", SearchOption.TopDirectoryOnly))
                     .OrderBy(f => f, StringComparer.OrdinalIgnoreCase))
        {
            cancellationToken.ThrowIfCancellationRequested();
            var content = await File.ReadAllTextAsync(file, cancellationToken);
            var doc = yaml.DeserializeAgentYamlDocument<ToolYamlDocument>(content);
            if (string.IsNullOrWhiteSpace(doc.Name))
            {
                logger.LogWarning("Skipping tool YAML without name: {File}", file);
                continue;
            }

            entries.Add(new ToolCatalogEntry(
                Name: doc.Name.Trim(),
                Description: (doc.Description ?? string.Empty).Trim(),
                Mcp: doc.Mcp,
                TimeoutMs: doc.TimeoutMs,
                SideEffect: doc.SideEffect,
                InputSchema: ToJsonElement(doc.InputSchema)));
        }

        _cache["all"] = entries;
        return entries;
    }

    private static JsonElement ToJsonElement(object? inputSchema)
    {
        if (inputSchema is null)
        {
            return EmptyObjectSchema;
        }

        try
        {
            var json = JsonSerializer.Serialize(inputSchema);
            return JsonSerializer.Deserialize<JsonElement>(json);
        }
        catch
        {
            return EmptyObjectSchema;
        }
    }

    private sealed class ToolYamlDocument
    {
        public string? Name { get; set; }
        public string? Description { get; set; }
        public bool Mcp { get; set; }
        public int? TimeoutMs { get; set; }
        public bool SideEffect { get; set; }
        public object? InputSchema { get; set; }
    }
}
