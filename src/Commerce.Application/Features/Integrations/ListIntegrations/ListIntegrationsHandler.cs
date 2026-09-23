using Microsoft.EntityFrameworkCore;
using Commerce.Application.Abstracts;
using Commerce.Application.Common;
using Commerce.Application.Common.Abstracts;

namespace Commerce.Application.Features.Integrations.ListIntegrations;

public sealed class ListIntegrationsHandler(
    ICommerceDbContext db,
    IWorkspaceContext workspaceContext,
    IIntegrationsMetadataService metadataService)
    : IQueryHandler<ListIntegrationsQuery, ListIntegrationsResult>
{
    public async Task<ListIntegrationsResult> Handle(
        ListIntegrationsQuery request,
        CancellationToken cancellationToken)
    {
        var workspaceId = await WorkspaceRequirement.RequireWorkspaceIdAsync(
            workspaceContext, db, cancellationToken);

        var rows = await db.Integrations
            .AsNoTracking()
            .Where(i => i.WorkspaceId == workspaceId)
            .OrderByDescending(i => i.CreatedAt)
            .Select(i => new
            {
                i.Id,
                i.WorkspaceId,
                i.Provider,
                i.Name,
                i.IsActive,
                i.CreatedAt
            })
            .ToListAsync(cancellationToken);

        var allMeta = await metadataService.GetAvailableIntegrationsAsync(cancellationToken);
        var metaByProvider = allMeta.ToDictionary(m => m.Key, StringComparer.OrdinalIgnoreCase);

        var items = rows.Select(i =>
        {
            var logo = metaByProvider.TryGetValue(i.Provider, out var m) ? m.LogoUrl : null;
            return new IntegrationListItem(
                i.Id,
                i.WorkspaceId,
                i.Provider,
                i.Name,
                logo,
                i.IsActive,
                i.CreatedAt);
        }).ToList();

        return new ListIntegrationsResult(items);
    }
}
