namespace Commerce.Application.Features.Workspaces.GetWorkspace;

public sealed record GetWorkspaceResult(
    Guid Id,
    string Name,
    bool IsDefault,
    bool IsSystem,
    string EnvironmentKind,
    DateTimeOffset CreatedAt);
