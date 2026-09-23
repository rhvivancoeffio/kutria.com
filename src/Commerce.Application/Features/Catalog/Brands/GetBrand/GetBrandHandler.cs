using Microsoft.Extensions.Logging;
using Commerce.Application.Abstracts;
using Commerce.Application.Common;
using Commerce.Application.Common.Abstracts;

namespace Commerce.Application.Features.Catalog.Brands.GetBrand;

public sealed class GetBrandHandler(
    ICommerceDbContext db,
    IWorkspaceContext workspaceContext,
    IGravityStoreDataClient gravityStore,
    ILogger<GetBrandHandler> logger)
    : IQueryHandler<GetBrandQuery, GetBrandResult?>
{
    public async Task<GetBrandResult?> Handle(GetBrandQuery request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.BrandId))
            return null;

        var workspaceId = await WorkspaceRequirement.RequireWorkspaceIdAsync(
            workspaceContext, db, cancellationToken);
        var (_, credentials) = await CatalogGravityCredentials.LoadActiveAsync(
            db,
            logger,
            workspaceId,
            request.IntegrationId,
            cancellationToken);

        var item = await gravityStore.GetBrandAsync(credentials, request.BrandId.Trim(), cancellationToken);
        return item is null
            ? null
            : new GetBrandResult(item.BrandId, item.Name, item.IsActive);
    }
}
