using Microsoft.Extensions.Logging;
using Commerce.Application.Abstracts;
using Commerce.Application.Common;
using Commerce.Application.Common.Abstracts;

namespace Commerce.Application.Features.Catalog.Categories.CreateCategory;

public sealed class CreateCategoryHandler(
    ICommerceDbContext db,
    IWorkspaceContext workspaceContext,
    IGravityStoreDataClient gravityStore,
    ILogger<CreateCategoryHandler> logger)
    : ICommandHandler<CreateCategoryCommand, CreateCategoryResult>
{
    public async Task<CreateCategoryResult> Handle(CreateCategoryCommand request, CancellationToken cancellationToken)
    {
        var workspaceId = await WorkspaceRequirement.RequireWorkspaceIdAsync(
            workspaceContext, db, cancellationToken);
        var (_, credentials) = await CatalogGravityCredentials.LoadActiveAsync(
            db,
            logger,
            workspaceId,
            request.IntegrationId,
            cancellationToken);

        var created = await gravityStore.CreateCategoryAsync(
            credentials,
            new GravityCategoryCreateRequest(
                request.Name.Trim(),
                string.IsNullOrWhiteSpace(request.Slug) ? null : request.Slug.Trim(),
                string.IsNullOrWhiteSpace(request.Description) ? null : request.Description.Trim(),
                string.IsNullOrWhiteSpace(request.ParentCategoryId) ? null : request.ParentCategoryId.Trim(),
                request.IsActive),
            cancellationToken);

        return new CreateCategoryResult(
            created.CategoryId,
            created.Name,
            string.IsNullOrWhiteSpace(created.ParentId) ? null : created.ParentId,
            created.Url);
    }
}
