using Microsoft.Extensions.Logging;
using Commerce.Application.Abstracts;
using Commerce.Application.Common;
using Commerce.Application.Common.Abstracts;

namespace Commerce.Application.Features.Catalog.Brands.AutocompleteBrands;

public sealed class AutocompleteBrandsHandler(
    ICommerceDbContext db,
    IWorkspaceContext workspaceContext,
    IGravityStoreDataClient gravityStore,
    ILogger<AutocompleteBrandsHandler> logger)
    : IQueryHandler<AutocompleteBrandsQuery, AutocompleteBrandsResult>
{
    public async Task<AutocompleteBrandsResult> Handle(
        AutocompleteBrandsQuery request,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Name) || request.Name.Trim().Length < 1)
            return new AutocompleteBrandsResult([]);

        var workspaceId = await WorkspaceRequirement.RequireWorkspaceIdAsync(
            workspaceContext, db, cancellationToken);
        var (_, credentials) = await CatalogGravityCredentials.LoadActiveAsync(
            db,
            logger,
            workspaceId,
            request.IntegrationId,
            cancellationToken);

        var items = await gravityStore.AutocompleteBrandsAsync(
            credentials,
            request.Name.Trim(),
            cancellationToken);

        return new AutocompleteBrandsResult(
            items.Select(b => new BrandAutocompleteItem(b.BrandId, b.Name)).ToList());
    }
}
