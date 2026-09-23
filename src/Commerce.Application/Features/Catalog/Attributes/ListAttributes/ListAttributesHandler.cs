using Microsoft.Extensions.Logging;
using Commerce.Application.Abstracts;
using Commerce.Application.Common;
using Commerce.Application.Common.Abstracts;

namespace Commerce.Application.Features.Catalog.Attributes.ListAttributes;

public sealed class ListAttributesHandler(
    ICommerceDbContext db,
    IWorkspaceContext workspaceContext,
    IGravityStoreDataClient gravityStore,
    ILogger<ListAttributesHandler> logger)
    : IQueryHandler<ListAttributesQuery, ListAttributesResult>
{
    public async Task<ListAttributesResult> Handle(ListAttributesQuery request, CancellationToken cancellationToken)
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
        var items = await gravityStore.ListEntityAttributesAsync(
            credentials,
            request.Name,
            page,
            pageSize,
            cancellationToken);

        return new ListAttributesResult(
            items.Select(a => new AttributeListItem(
                a.EntityAttributeId,
                a.EntityName,
                a.Key,
                a.Name,
                a.Label,
                a.SpecificationType,
                a.SpecificationTypeName,
                a.IsMultiOption,
                a.Required,
                a.IsPublic,
                a.Order,
                a.SectionGroup)).ToList());
    }
}
