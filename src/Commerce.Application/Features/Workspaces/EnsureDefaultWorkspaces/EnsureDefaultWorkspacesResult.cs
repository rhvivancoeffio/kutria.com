namespace Commerce.Application.Features.Workspaces.EnsureDefaultWorkspaces;

public sealed record EnsureDefaultWorkspacesResult(Guid SandboxId, Guid ProductionId, bool Created);
