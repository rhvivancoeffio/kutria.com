using Commerce.Application.Common.Abstracts;
using Commerce.Domain.Workspaces;

namespace Commerce.Application.Features.Workspaces.UpdateWorkspace;

public sealed record UpdateWorkspaceCommand(
    Guid Id,
    string Name,
    WorkspaceEnvironment EnvironmentKind,
    bool IsDefault) : ICommand<UpdateWorkspaceResult>;
