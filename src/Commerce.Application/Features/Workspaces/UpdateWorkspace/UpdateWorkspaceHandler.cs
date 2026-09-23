using Microsoft.EntityFrameworkCore;
using Commerce.Application.Abstracts;
using Commerce.Application.Common.Abstracts;

namespace Commerce.Application.Features.Workspaces.UpdateWorkspace;

public sealed class UpdateWorkspaceHandler(ICommerceDbContext db)
    : ICommandHandler<UpdateWorkspaceCommand, UpdateWorkspaceResult>
{
    public async Task<UpdateWorkspaceResult> Handle(UpdateWorkspaceCommand request, CancellationToken cancellationToken)
    {
        var workspace = await db.Workspaces.FirstOrDefaultAsync(w => w.Id == request.Id, cancellationToken)
            ?? throw new KeyNotFoundException($"Workspace {request.Id} was not found.");

        var nameTaken = await db.Workspaces.AnyAsync(
            w => w.Name == request.Name && w.Id != request.Id,
            cancellationToken);
        if (nameTaken)
        {
            throw new InvalidOperationException($"Workspace '{request.Name}' already exists.");
        }

        if (request.IsDefault && !workspace.IsDefault)
        {
            var currentDefaults = await db.Workspaces.Where(w => w.IsDefault).ToListAsync(cancellationToken);
            foreach (var item in currentDefaults)
            {
                item.IsDefault = false;
            }
        }

        workspace.Name = request.Name.Trim();
        workspace.EnvironmentKind = request.EnvironmentKind;
        workspace.IsDefault = request.IsDefault;

        await db.SaveChangesAsync(cancellationToken);

        return new UpdateWorkspaceResult(
            workspace.Id,
            workspace.Name,
            workspace.IsDefault,
            workspace.IsSystem,
            workspace.EnvironmentKind.ToString());
    }
}
