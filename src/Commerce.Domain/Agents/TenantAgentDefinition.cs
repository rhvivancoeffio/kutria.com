using Commerce.Domain.Common;

namespace Commerce.Domain.Agents;

public class TenantAgentDefinition : BaseEntity, ITenantScoped
{
    public string TenantId { get; set; } = string.Empty;
    public string AgentKey { get; set; } = string.Empty;
    public string Kind { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Instructions { get; set; } = string.Empty;
    public string? Model { get; set; }
    public string? Queue { get; set; }
    public List<string> Tools { get; set; } = [];
    public List<string> Publishes { get; set; } = [];
    public string Yaml { get; set; } = string.Empty;
    public string BasedOnHash { get; set; } = string.Empty;
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;
}
