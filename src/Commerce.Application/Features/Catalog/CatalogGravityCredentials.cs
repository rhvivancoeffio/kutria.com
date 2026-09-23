using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Commerce.Application.Abstracts;
using Commerce.Application.Features.DataIngestion;

namespace Commerce.Application.Features.Catalog;

/// <summary>
/// Resolves the workspace's active Gravity / GravityAPI integration for catalog proxies (brands, categories, …).
/// </summary>
public static class CatalogGravityCredentials
{
    public static async Task<(Domain.Integrations.Integration Integration, GravityStoreCredentials Credentials)> LoadActiveAsync(
        ICommerceDbContext db,
        ILogger logger,
        Guid workspaceId,
        Guid? integrationId = null,
        CancellationToken cancellationToken = default)
    {
        Domain.Integrations.Integration? integration;
        if (integrationId is Guid id && id != Guid.Empty)
        {
            integration = await db.Integrations.AsNoTracking()
                .FirstOrDefaultAsync(
                    x => x.Id == id && x.IsActive && x.WorkspaceId == workspaceId,
                    cancellationToken);
        }
        else
        {
            integration = await db.Integrations.AsNoTracking()
                .Where(x => x.IsActive
                    && x.WorkspaceId == workspaceId
                    && (x.Provider == "Gravity" || x.Provider == "GravityAPI"))
                .OrderByDescending(x => x.CreatedAt)
                .FirstOrDefaultAsync(cancellationToken);
        }

        if (integration is null)
            throw new InvalidOperationException("No active Gravity integration found for this workspace.");

        if (!DataIngestCredentials.TryParse(integration.SettingsJson, out var credentials, out var reason))
        {
            logger.LogWarning(
                "Gravity credentials parse failed for catalog. IntegrationId={IntegrationId} Reason={Reason}",
                integration.Id,
                reason);
            throw new InvalidOperationException(
                $"Integration does not have Gravity API credentials (url + apiKey + organizationId). {reason}");
        }

        return (integration, credentials);
    }
}
