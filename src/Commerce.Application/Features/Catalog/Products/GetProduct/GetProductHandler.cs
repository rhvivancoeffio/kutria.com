using Microsoft.Extensions.Logging;
using Commerce.Application.Abstracts;
using Commerce.Application.Common;
using Commerce.Application.Common.Abstracts;

namespace Commerce.Application.Features.Catalog.Products.GetProduct;

public sealed class GetProductHandler(
    ICommerceDbContext db,
    IWorkspaceContext workspaceContext,
    IGravityStoreDataClient gravityStore,
    ILogger<GetProductHandler> logger)
    : IQueryHandler<GetProductQuery, GetProductResult?>
{
    public async Task<GetProductResult?> Handle(GetProductQuery request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.ProductId))
            return null;

        var workspaceId = await WorkspaceRequirement.RequireWorkspaceIdAsync(
            workspaceContext, db, cancellationToken);
        var (_, credentials) = await CatalogGravityCredentials.LoadActiveAsync(
            db,
            logger,
            workspaceId,
            request.IntegrationId,
            cancellationToken);

        var item = await gravityStore.GetProductByIdAsync(credentials, request.ProductId.Trim(), cancellationToken);
        return item is null
            ? null
            : new GetProductResult(
                item.ProductId,
                item.Name,
                item.ProductStatusName,
                item.UpdatedOn,
                item.PayloadJson);
    }
}
