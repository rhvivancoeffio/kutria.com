namespace Commerce.Application.Features.Workspaces.UpdateWorkspace;

public sealed record UpdateWorkspaceResult(
    Guid Id,
    string Name,
    bool IsDefault,
    bool IsSystem,
    string EnvironmentKind);
