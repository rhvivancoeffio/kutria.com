namespace Commerce.Application.Features.Workspaces.ListWorkspaces;

public sealed record ListWorkspacesResult(IReadOnlyList<WorkspaceListItem> Items);
