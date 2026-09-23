using Commerce.Application.Common.Abstracts;

namespace Commerce.Application.Features.Workspaces.DeleteWorkspace;

public sealed record DeleteWorkspaceCommand(Guid Id) : ICommand<DeleteWorkspaceResult>;
