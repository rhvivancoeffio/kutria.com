namespace Commerce.Application.Features.Integrations.GetIntegration;

public sealed record GetIntegrationResult(
    Guid Id,
    Guid? WorkspaceId,
    string Provider,
    string Name,
    Dictionary<string, string> Settings,
    bool IsActive,
    DateTimeOffset CreatedAt);
