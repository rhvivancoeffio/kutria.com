using Microsoft.Extensions.Logging;
using Commerce.Application.Abstracts;
using Commerce.Application.Common;
using Commerce.Application.Common.Abstracts;

namespace Commerce.Application.Features.Catalog.Attributes.DeleteAttribute;

public sealed class DeleteAttributeHandler(
    ICommerceDbContext db,
    IWorkspaceContext workspaceContext,
    IGravityStoreDataClient gravityStore,
    ILogger<DeleteAttributeHandler> logger)
    : ICommandHandler<DeleteAttributeCommand, DeleteAttributeResult>
{
    public async Task<DeleteAttributeResult> Handle(DeleteAttributeCommand request, CancellationToken cancellationToken)
    {
        var workspaceId = await WorkspaceRequirement.RequireWorkspaceIdAsync(
            workspaceContext, db, cancellationToken);
        var (_, credentials) = await CatalogGravityCredentials.LoadActiveAsync(
            db,
            logger,
            workspaceId,
            request.IntegrationId,
            cancellationToken);

        await gravityStore.DeleteEntityAttributeAsync(
            credentials,
            request.EntityAttributeId.Trim(),
            cancellationToken);
        return new DeleteAttributeResult(request.EntityAttributeId.Trim(), true);
    }
}
