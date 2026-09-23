namespace Commerce.Domain.Mcp;

public class McpOAuthAuthorizationCode
{
    public string Code { get; set; } = string.Empty;
    public string ClientId { get; set; } = string.Empty;
    public string RedirectUri { get; set; } = string.Empty;
    public string CodeChallenge { get; set; } = string.Empty;
    public string CodeChallengeMethod { get; set; } = "S256";
    public string? Scope { get; set; }
    public string? Resource { get; set; }
    public string UserId { get; set; } = string.Empty;
    public string TenantId { get; set; } = string.Empty;
    public DateTime ExpiresAt { get; set; }
}
