using Microsoft.Extensions.Logging;
using Commerce.Application.Abstracts;
using Commerce.Application.Common;
using Commerce.Application.Common.Abstracts;

namespace Commerce.Application.Features.Catalog.Attributes.CreateAttribute;

public sealed class CreateAttributeHandler(
    ICommerceDbContext db,
    IWorkspaceContext workspaceContext,
    IGravityStoreDataClient gravityStore,
    ILogger<CreateAttributeHandler> logger)
    : ICommandHandler<CreateAttributeCommand, CreateAttributeResult>
{
    public async Task<CreateAttributeResult> Handle(CreateAttributeCommand request, CancellationToken cancellationToken)
    {
        var workspaceId = await WorkspaceRequirement.RequireWorkspaceIdAsync(
            workspaceContext, db, cancellationToken);
        var (_, credentials) = await CatalogGravityCredentials.LoadActiveAsync(
            db,
            logger,
            workspaceId,
            request.IntegrationId,
            cancellationToken);

        var created = await gravityStore.CreateEntityAttributeAsync(
            credentials,
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

        return new CreateAttributeResult(
            created.EntityAttributeId,
            created.EntityName,
            created.Name,
            created.Label,
            created.SpecificationType,
            created.SpecificationTypeName);
    }
}
