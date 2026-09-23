namespace Commerce.Application.Features.Workspaces.CreateWorkspace;

public sealed record CreateWorkspaceResult(
    Guid Id,
    string Name,
    bool IsDefault,
    bool IsSystem,
    string EnvironmentKind,
    DateTimeOffset CreatedAt);
