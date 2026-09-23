using Microsoft.Extensions.Logging;
using Commerce.Application.Abstracts;
using Commerce.Application.Common;
using Commerce.Application.Common.Abstracts;

namespace Commerce.Application.Features.Catalog.Brands.ListBrands;

public sealed class ListBrandsHandler(
    ICommerceDbContext db,
    IWorkspaceContext workspaceContext,
    IGravityStoreDataClient gravityStore,
    ILogger<ListBrandsHandler> logger)
    : IQueryHandler<ListBrandsQuery, ListBrandsResult>
{
    public async Task<ListBrandsResult> Handle(ListBrandsQuery request, CancellationToken cancellationToken)
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
        var items = await gravityStore.ListBrandsAsync(
            credentials,
            request.Name,
            page,
            pageSize,
            cancellationToken);

        return new ListBrandsResult(
            items.Select(b => new BrandListItem(b.BrandId, b.Name, b.IsActive)).ToList());
    }
}
