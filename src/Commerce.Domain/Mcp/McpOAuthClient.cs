namespace Commerce.Domain.Mcp;

public class McpOAuthClient
{
    public string ClientId { get; set; } = string.Empty;
    public string RedirectUrisJson { get; set; } = "[]";
    public string? GrantTypesJson { get; set; }
    public string? ResponseTypesJson { get; set; }
    public string? ClientName { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
