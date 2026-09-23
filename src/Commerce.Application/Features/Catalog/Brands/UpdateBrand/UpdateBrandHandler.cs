using Microsoft.Extensions.Logging;
using Commerce.Application.Abstracts;
using Commerce.Application.Common;
using Commerce.Application.Common.Abstracts;

namespace Commerce.Application.Features.Catalog.Brands.UpdateBrand;

public sealed class UpdateBrandHandler(
    ICommerceDbContext db,
    IWorkspaceContext workspaceContext,
    IGravityStoreDataClient gravityStore,
    ILogger<UpdateBrandHandler> logger)
    : ICommandHandler<UpdateBrandCommand, UpdateBrandResult>
{
    public async Task<UpdateBrandResult> Handle(UpdateBrandCommand request, CancellationToken cancellationToken)
    {
        var workspaceId = await WorkspaceRequirement.RequireWorkspaceIdAsync(
            workspaceContext, db, cancellationToken);
        var (_, credentials) = await CatalogGravityCredentials.LoadActiveAsync(
            db,
            logger,
            workspaceId,
            request.IntegrationId,
            cancellationToken);

        var updated = await gravityStore.UpdateBrandAsync(
            credentials,
            request.BrandId.Trim(),
            new GravityBrandCreateRequest(
                request.Name.Trim(),
                string.IsNullOrWhiteSpace(request.Description) ? null : request.Description.Trim(),
                request.IsActive,
                string.IsNullOrWhiteSpace(request.ImageUrl) ? null : request.ImageUrl.Trim()),
            cancellationToken);

        return new UpdateBrandResult(updated.BrandId, updated.Name, updated.IsActive);
    }
}
