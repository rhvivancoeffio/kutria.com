namespace Commerce.Application.Features.Workspaces.ListWorkspaces;

public sealed record WorkspaceListItem(
    Guid Id,
    string Name,
    bool IsDefault,
    bool IsSystem,
    string EnvironmentKind,
    DateTimeOffset CreatedAt);
