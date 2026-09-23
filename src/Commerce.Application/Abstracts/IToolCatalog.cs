using System.Text.Json;

namespace Commerce.Application.Abstracts;

public interface IToolCatalog
{
    Task<IReadOnlyList<ToolCatalogEntry>> GetAllAsync(CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ToolCatalogEntry>> GetMcpToolsAsync(CancellationToken cancellationToken = default);

    Task<ToolCatalogEntry?> GetByNameAsync(string name, CancellationToken cancellationToken = default);

    /// <summary>
    /// Sync lookup (uses warm cache). Prefers YAML description; falls back when missing.
    /// </summary>
    string ResolveDescription(string name, string? fallback = null);
}

public sealed record ToolCatalogEntry(
    string Name,
    string Description,
    bool Mcp,
    int? TimeoutMs,
    bool SideEffect,
    JsonElement InputSchema);
