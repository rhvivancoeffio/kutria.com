using Microsoft.Extensions.Logging;
using Commerce.Application.Abstracts;
using Commerce.Application.Common;
using Commerce.Application.Common.Abstracts;

namespace Commerce.Application.Features.Catalog.Attributes.GetAttribute;

public sealed class GetAttributeHandler(
    ICommerceDbContext db,
    IWorkspaceContext workspaceContext,
    IGravityStoreDataClient gravityStore,
    ILogger<GetAttributeHandler> logger)
    : IQueryHandler<GetAttributeQuery, GetAttributeResult?>
{
    public async Task<GetAttributeResult?> Handle(GetAttributeQuery request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.EntityAttributeId))
            return null;

        var workspaceId = await WorkspaceRequirement.RequireWorkspaceIdAsync(
            workspaceContext, db, cancellationToken);
        var (_, credentials) = await CatalogGravityCredentials.LoadActiveAsync(
            db,
            logger,
            workspaceId,
            request.IntegrationId,
            cancellationToken);

        var item = await gravityStore.GetEntityAttributeAsync(
            credentials,
            request.EntityAttributeId.Trim(),
            cancellationToken);

        return item is null
            ? null
            : new GetAttributeResult(
                item.EntityAttributeId,
                item.EntityName,
                item.Key,
                item.Name,
                item.Description,
                item.Label,
                item.Values,
                item.IsMultiOption,
                item.SpecificationType,
                item.SpecificationTypeName,
                item.Order,
                item.Required,
                item.IsPublic,
                item.SectionGroup);
    }
}
