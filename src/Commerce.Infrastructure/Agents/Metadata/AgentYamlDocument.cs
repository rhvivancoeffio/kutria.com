namespace Commerce.Infrastructure.Agents.Metadata;

public sealed class AgentYamlDocument
{
    public string? Key { get; set; }
    public string? Version { get; set; }
    public string? Kind { get; set; }
    public string? Name { get; set; }
    public string? Description { get; set; }
    public string? Audience { get; set; }
    public string? Instructions { get; set; }
    public AgentYamlPrompt? Prompt { get; set; }
    public AgentYamlModel? Model { get; set; }
    public AgentYamlGuardrails? Guardrails { get; set; }
    public AgentYamlOutput? Output { get; set; }
    public string? Queue { get; set; }
    public List<AgentYamlTool>? Tools { get; set; }
    public List<string>? Publishes { get; set; }
}

public sealed class AgentYamlPrompt
{
    public string? SystemFile { get; set; }
}

public sealed class AgentYamlModel
{
    public string? Name { get; set; }
    public double? Temperature { get; set; }
    public int? MaxTokens { get; set; }
}

public sealed class AgentYamlGuardrails
{
    public bool NoInvent { get; set; }
    public List<string>? ForbiddenPhrases { get; set; }
}

public sealed class AgentYamlOutput
{
    public string? SchemaFile { get; set; }
}

public sealed class AgentYamlTool
{
    public string? Name { get; set; }
    public int? TimeoutMs { get; set; }
    public bool? SideEffect { get; set; }
    public List<string>? Requires { get; set; }
}
