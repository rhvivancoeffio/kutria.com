namespace Commerce.Application.Configuration;

public sealed class McpOptions
{
    public const string SectionName = "Mcp";

    public static readonly string[] DefaultAllowedRedirectUriPrefixes =
    [
        "https://chatgpt.com/",
        "https://platform.openai.com/",
        "https://claude.ai/",
        "https://console.anthropic.com/",
        "https://oauth.pstmn.io/",
        "http://localhost"
    ];

    /// <summary>Public MCP host URL (e.g. https://mcp.kutria.com).</summary>
    public string PublicBaseUrl { get; set; } = "https://mcp.kutria.com";

    public string FrontendBaseUrl { get; set; } = "https://kutria.com";

    public bool Enabled { get; set; } = true;

    public string[] AllowedRedirectUriPrefixes { get; set; } = [];

    public int RefreshTokenExpirationDays { get; set; } = 30;

    public int AccessTokenExpirationMinutes { get; set; } = 60;

    public string[] GetAllowedRedirectUriPrefixes()
        => AllowedRedirectUriPrefixes is { Length: > 0 }
            ? AllowedRedirectUriPrefixes
            : DefaultAllowedRedirectUriPrefixes;
}
