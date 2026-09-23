namespace Commerce.Application.Abstracts;

public sealed record AgentDefinition(
    string Key,
    string Kind,
    string Name,
    string Instructions,
    string? Model,
    string? Queue,
    IReadOnlyList<string> Tools,
    IReadOnlyList<string> Publishes,
    string Yaml,
    string Hash,
    bool IsTenantOverride,
    float? Temperature = null,
    int? MaxOutputTokens = null,
    string OutputSchema = "",
    IReadOnlyList<string>? ForbiddenPhrases = null,
    string Description = "",
    string Audience = "buyer",
    IReadOnlyDictionary<string, IReadOnlyList<string>>? ToolRequires = null);
