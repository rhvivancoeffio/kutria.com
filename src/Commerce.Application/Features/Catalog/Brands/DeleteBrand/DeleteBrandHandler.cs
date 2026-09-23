using Microsoft.Extensions.Logging;
using Commerce.Application.Abstracts;
using Commerce.Application.Common;
using Commerce.Application.Common.Abstracts;

namespace Commerce.Application.Features.Catalog.Brands.DeleteBrand;

public sealed class DeleteBrandHandler(
    ICommerceDbContext db,
    IWorkspaceContext workspaceContext,
    IGravityStoreDataClient gravityStore,
    ILogger<DeleteBrandHandler> logger)
    : ICommandHandler<DeleteBrandCommand, DeleteBrandResult>
{
    public async Task<DeleteBrandResult> Handle(DeleteBrandCommand request, CancellationToken cancellationToken)
    {
        var workspaceId = await WorkspaceRequirement.RequireWorkspaceIdAsync(
            workspaceContext, db, cancellationToken);
        var (_, credentials) = await CatalogGravityCredentials.LoadActiveAsync(
            db,
            logger,
            workspaceId,
            request.IntegrationId,
            cancellationToken);

        await gravityStore.DeleteBrandAsync(credentials, request.BrandId.Trim(), cancellationToken);
        return new DeleteBrandResult(request.BrandId.Trim(), true);
    }
}
