namespace Commerce.Application.Features.Integrations.CreateIntegration;

public sealed record CreateIntegrationResult(
    Guid Id,
    Guid? WorkspaceId,
    string Provider,
    string Name,
    bool IsActive,
    DateTimeOffset CreatedAt);
