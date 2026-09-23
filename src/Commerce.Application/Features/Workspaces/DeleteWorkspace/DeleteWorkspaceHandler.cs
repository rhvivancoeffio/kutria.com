using Microsoft.EntityFrameworkCore;
using Commerce.Application.Abstracts;
using Commerce.Application.Common.Abstracts;

namespace Commerce.Application.Features.Workspaces.DeleteWorkspace;

public sealed class DeleteWorkspaceHandler(ICommerceDbContext db)
    : ICommandHandler<DeleteWorkspaceCommand, DeleteWorkspaceResult>
{
    public async Task<DeleteWorkspaceResult> Handle(DeleteWorkspaceCommand request, CancellationToken cancellationToken)
    {
        var workspace = await db.Workspaces.FirstOrDefaultAsync(w => w.Id == request.Id, cancellationToken)
            ?? throw new KeyNotFoundException($"Workspace {request.Id} was not found.");

        if (workspace.IsSystem)
        {
            throw new InvalidOperationException("System workspaces cannot be deleted.");
        }

        db.Workspaces.Remove(workspace);
        await db.SaveChangesAsync(cancellationToken);
        return new DeleteWorkspaceResult(request.Id, true);
    }
}
