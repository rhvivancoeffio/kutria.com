using Microsoft.Extensions.Logging;
using Commerce.Application.Abstracts;
using Commerce.Application.Common;
using Commerce.Application.Common.Abstracts;

namespace Commerce.Application.Features.Catalog.Categories.UpdateCategory;

public sealed class UpdateCategoryHandler(
    ICommerceDbContext db,
    IWorkspaceContext workspaceContext,
    IGravityStoreDataClient gravityStore,
    ILogger<UpdateCategoryHandler> logger)
    : ICommandHandler<UpdateCategoryCommand, UpdateCategoryResult>
{
    public async Task<UpdateCategoryResult> Handle(UpdateCategoryCommand request, CancellationToken cancellationToken)
    {
        var workspaceId = await WorkspaceRequirement.RequireWorkspaceIdAsync(
            workspaceContext, db, cancellationToken);
        var (_, credentials) = await CatalogGravityCredentials.LoadActiveAsync(
            db,
            logger,
            workspaceId,
            request.IntegrationId,
            cancellationToken);

        var updated = await gravityStore.UpdateCategoryAsync(
            credentials,
            request.CategoryId.Trim(),
            new GravityCategoryCreateRequest(
                request.Name.Trim(),
                string.IsNullOrWhiteSpace(request.Slug) ? null : request.Slug.Trim(),
                string.IsNullOrWhiteSpace(request.Description) ? null : request.Description.Trim(),
                string.IsNullOrWhiteSpace(request.ParentCategoryId) ? null : request.ParentCategoryId.Trim(),
                request.IsActive),
            cancellationToken);

        return new UpdateCategoryResult(
            updated.CategoryId,
            updated.Name,
            string.IsNullOrWhiteSpace(updated.ParentId) ? null : updated.ParentId,
            updated.Url);
    }
}
