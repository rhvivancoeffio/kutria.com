namespace Commerce.Application.Features.Integrations.ListIntegrations;

public sealed record IntegrationListItem(
    Guid Id,
    Guid? WorkspaceId,
    string Provider,
    string Name,
    string? LogoUrl,
    bool IsActive,
    DateTimeOffset CreatedAt);
