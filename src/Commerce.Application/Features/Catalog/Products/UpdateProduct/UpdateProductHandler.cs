using Microsoft.Extensions.Logging;
using Commerce.Application.Abstracts;
using Commerce.Application.Common;
using Commerce.Application.Common.Abstracts;

namespace Commerce.Application.Features.Catalog.Products.UpdateProduct;

public sealed class UpdateProductHandler(
    ICommerceDbContext db,
    IWorkspaceContext workspaceContext,
    IGravityStoreDataClient gravityStore,
    ILogger<UpdateProductHandler> logger)
    : ICommandHandler<UpdateProductCommand, UpdateProductResult>
{
    public async Task<UpdateProductResult> Handle(UpdateProductCommand request, CancellationToken cancellationToken)
    {
        var workspaceId = await WorkspaceRequirement.RequireWorkspaceIdAsync(
            workspaceContext, db, cancellationToken);
        var (_, credentials) = await CatalogGravityCredentials.LoadActiveAsync(
            db,
            logger,
            workspaceId,
            request.IntegrationId,
            cancellationToken);

        var updated = await gravityStore.UpdateProductAsync(
            credentials,
            request.ProductId.Trim(),
            new GravityProductCreateRequest(
                request.Name.Trim(),
                request.Description.Trim(),
                string.IsNullOrWhiteSpace(request.BrandId) ? null : request.BrandId.Trim(),
                string.IsNullOrWhiteSpace(request.BrandName) ? null : request.BrandName.Trim(),
                string.IsNullOrWhiteSpace(request.CategoryId) ? null : request.CategoryId.Trim(),
                string.IsNullOrWhiteSpace(request.CategoryPath) ? null : request.CategoryPath.Trim(),
                string.IsNullOrWhiteSpace(request.ImageUrl) ? null : request.ImageUrl.Trim(),
                request.IsActive,
                request.ShowInCatalog),
            cancellationToken);

        return new UpdateProductResult(updated.ProductId, updated.Name);
    }
}
