using Microsoft.EntityFrameworkCore;
using Commerce.Application.Abstracts;
using Commerce.Application.Common;
using Commerce.Application.Common.Abstracts;

namespace Commerce.Application.Features.Integrations.DeleteIntegration;

public sealed class DeleteIntegrationHandler(
    ICommerceDbContext db,
    IWorkspaceContext workspaceContext)
    : ICommandHandler<DeleteIntegrationCommand, DeleteIntegrationResult>
{
    public async Task<DeleteIntegrationResult> Handle(
        DeleteIntegrationCommand request,
        CancellationToken cancellationToken)
    {
        var workspaceId = await WorkspaceRequirement.RequireWorkspaceIdAsync(
            workspaceContext, db, cancellationToken);

        var integration = await db.Integrations
                .FirstOrDefaultAsync(i => i.Id == request.Id && i.WorkspaceId == workspaceId, cancellationToken)
            ?? throw new KeyNotFoundException($"Integration {request.Id} was not found.");

        db.Integrations.Remove(integration);
        await db.SaveChangesAsync(cancellationToken);
        return new DeleteIntegrationResult(request.Id, true);
    }
}
