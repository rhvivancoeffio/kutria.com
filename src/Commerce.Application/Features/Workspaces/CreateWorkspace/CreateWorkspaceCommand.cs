using Commerce.Application.Common.Abstracts;
using Commerce.Domain.Workspaces;

namespace Commerce.Application.Features.Workspaces.CreateWorkspace;

public sealed record CreateWorkspaceCommand(
    string Name,
    WorkspaceEnvironment EnvironmentKind = WorkspaceEnvironment.Sandbox,
    bool IsDefault = false) : ICommand<CreateWorkspaceResult>;
