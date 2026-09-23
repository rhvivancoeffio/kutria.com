using Microsoft.Extensions.Logging;
using Commerce.Application.Abstracts;
using Commerce.Application.Common;
using Commerce.Application.Common.Abstracts;

namespace Commerce.Application.Features.Catalog.Categories.GetCategory;

public sealed class GetCategoryHandler(
    ICommerceDbContext db,
    IWorkspaceContext workspaceContext,
    IGravityStoreDataClient gravityStore,
    ILogger<GetCategoryHandler> logger)
    : IQueryHandler<GetCategoryQuery, GetCategoryResult?>
{
    public async Task<GetCategoryResult?> Handle(GetCategoryQuery request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.CategoryId))
            return null;

        var workspaceId = await WorkspaceRequirement.RequireWorkspaceIdAsync(
            workspaceContext, db, cancellationToken);
        var (_, credentials) = await CatalogGravityCredentials.LoadActiveAsync(
            db,
            logger,
            workspaceId,
            request.IntegrationId,
            cancellationToken);

        var item = await gravityStore.GetCategoryAsync(credentials, request.CategoryId.Trim(), cancellationToken);
        return item is null
            ? null
            : new GetCategoryResult(
                item.CategoryId,
                item.Name,
                string.IsNullOrWhiteSpace(item.ParentId) ? null : item.ParentId,
                item.Url);
    }
}
