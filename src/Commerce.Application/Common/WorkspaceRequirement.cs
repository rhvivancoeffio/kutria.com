using Commerce.Application.Abstracts;
using Microsoft.EntityFrameworkCore;

namespace Commerce.Application.Common;

public static class WorkspaceRequirement
{
    /// <summary>
    /// Resolves the active workspace from <see cref="IWorkspaceContext"/> and ensures it exists for the current tenant.
    /// </summary>
    public static async Task<Guid> RequireWorkspaceIdAsync(
        IWorkspaceContext workspaceContext,
        ICommerceDbContext db,
        CancellationToken cancellationToken = default)
    {
        var workspaceId = workspaceContext.WorkspaceId
            ?? throw new InvalidOperationException("Workspace is required.");

        var exists = await db.Workspaces.AnyAsync(w => w.Id == workspaceId, cancellationToken);
        if (!exists)
            throw new KeyNotFoundException($"Workspace {workspaceId} was not found.");

        return workspaceId;
    }
}
