namespace Commerce.Application.Features.Integrations.UpdateIntegration;

public sealed record UpdateIntegrationResult(
    Guid Id,
    Guid? WorkspaceId,
    string Provider,
    string Name,
    bool IsActive);
