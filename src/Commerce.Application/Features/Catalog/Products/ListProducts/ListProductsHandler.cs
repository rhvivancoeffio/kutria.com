using Microsoft.Extensions.Logging;
using Commerce.Application.Abstracts;
using Commerce.Application.Common;
using Commerce.Application.Common.Abstracts;

namespace Commerce.Application.Features.Catalog.Products.ListProducts;

public sealed class ListProductsHandler(
    ICommerceDbContext db,
    IWorkspaceContext workspaceContext,
    IGravityStoreDataClient gravityStore,
    ILogger<ListProductsHandler> logger)
    : IQueryHandler<ListProductsQuery, ListProductsResult>
{
    public async Task<ListProductsResult> Handle(ListProductsQuery request, CancellationToken cancellationToken)
    {
        var workspaceId = await WorkspaceRequirement.RequireWorkspaceIdAsync(
            workspaceContext, db, cancellationToken);
        var (_, credentials) = await CatalogGravityCredentials.LoadActiveAsync(
            db,
            logger,
            workspaceId,
            request.IntegrationId,
            cancellationToken);

        var page = Math.Max(1, request.Page);
        var pageSize = Math.Clamp(request.PageSize, 1, 100);
        var result = await gravityStore.ListProductsAsync(credentials, page, pageSize, cancellationToken);

        return new ListProductsResult(
            result.CurrentPage,
            result.PageCount,
            result.PageSize,
            result.RowCount,
            result.Results.Select(p => new ProductListItem(
                p.ProductId,
                p.Name,
                p.ProductStatusName,
                p.UpdatedOn,
                p.ImageUrl,
                p.BrandName,
                p.CategoryPath,
                p.Stock,
                p.BasePrice,
                p.SpecialPrice,
                p.CurrencySymbol)).ToList());
    }
}
