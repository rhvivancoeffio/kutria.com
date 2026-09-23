using Microsoft.Extensions.Logging;
using Commerce.Application.Abstracts;
using Commerce.Application.Common;
using Commerce.Application.Common.Abstracts;

namespace Commerce.Application.Features.Catalog.Attributes.UpdateAttribute;

public sealed class UpdateAttributeHandler(
    ICommerceDbContext db,
    IWorkspaceContext workspaceContext,
    IGravityStoreDataClient gravityStore,
    ILogger<UpdateAttributeHandler> logger)
    : ICommandHandler<UpdateAttributeCommand, UpdateAttributeResult>
{
    public async Task<UpdateAttributeResult> Handle(UpdateAttributeCommand request, CancellationToken cancellationToken)
    {
        var workspaceId = await WorkspaceRequirement.RequireWorkspaceIdAsync(
            workspaceContext, db, cancellationToken);
        var (_, credentials) = await CatalogGravityCredentials.LoadActiveAsync(
            db,
            logger,
            workspaceId,
            request.IntegrationId,
            cancellationToken);

        var updated = await gravityStore.UpdateEntityAttributeAsync(
            credentials,
            request.EntityAttributeId.Trim(),
            new GravityEntityAttributeWriteRequest(
                request.EntityName.Trim(),
                request.Name.Trim(),
                request.IsMultiOption,
                request.SpecificationType,
                request.Required,
                string.IsNullOrWhiteSpace(request.Description) ? null : request.Description.Trim(),
                string.IsNullOrWhiteSpace(request.Label) ? null : request.Label.Trim(),
                string.IsNullOrWhiteSpace(request.Values) ? null : request.Values.Trim(),
                request.Order,
                request.IsPublic,
                string.IsNullOrWhiteSpace(request.SectionGroup) ? null : request.SectionGroup.Trim()),
            cancellationToken);

        return new UpdateAttributeResult(
            updated.EntityAttributeId,
            updated.EntityName,
            updated.Name,
            updated.Label,
            updated.SpecificationType,
            updated.SpecificationTypeName);
    }
}
