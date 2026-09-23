namespace Commerce.Application.Features.McpOAuth;

public sealed class McpOAuthException(string errorCode, string errorDescription) : Exception(errorDescription)
{
    public string ErrorCode { get; } = errorCode;
    public string ErrorDescription { get; } = errorDescription;
}
