using Microsoft.Extensions.Logging;
using Commerce.Application.Abstracts;
using Commerce.Application.Common;
using Commerce.Application.Common.Abstracts;

namespace Commerce.Application.Features.Catalog.Brands.CreateBrand;

public sealed class CreateBrandHandler(
    ICommerceDbContext db,
    IWorkspaceContext workspaceContext,
    IGravityStoreDataClient gravityStore,
    ILogger<CreateBrandHandler> logger)
    : ICommandHandler<CreateBrandCommand, CreateBrandResult>
{
    public async Task<CreateBrandResult> Handle(CreateBrandCommand request, CancellationToken cancellationToken)
    {
        var workspaceId = await WorkspaceRequirement.RequireWorkspaceIdAsync(
            workspaceContext, db, cancellationToken);
        var (_, credentials) = await CatalogGravityCredentials.LoadActiveAsync(
            db,
            logger,
            workspaceId,
            request.IntegrationId,
            cancellationToken);

        var created = await gravityStore.CreateBrandAsync(
            credentials,
            new GravityBrandCreateRequest(
                request.Name.Trim(),
                string.IsNullOrWhiteSpace(request.Description) ? null : request.Description.Trim(),
                request.IsActive,
                string.IsNullOrWhiteSpace(request.ImageUrl) ? null : request.ImageUrl.Trim()),
            cancellationToken);

        return new CreateBrandResult(created.BrandId, created.Name, created.IsActive);
    }
}
