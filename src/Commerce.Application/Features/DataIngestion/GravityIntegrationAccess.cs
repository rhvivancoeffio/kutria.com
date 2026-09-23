using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Commerce.Application.Abstracts;

namespace Commerce.Application.Features.DataIngestion;

internal static class GravityIntegrationAccess
{
    public static async Task<Domain.Integrations.Integration> EnsureExistsAsync(
        ICommerceDbContext db,
        Guid integrationId,
        CancellationToken cancellationToken)
    {
        var integration = await db.Integrations.AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == integrationId, cancellationToken)
            ?? throw new KeyNotFoundException($"Integration {integrationId} was not found.");

        if (!integration.IsActive)
            throw new InvalidOperationException("Integration is not active.");

        return integration;
    }

    public static async Task<(Domain.Integrations.Integration Integration, GravityStoreCredentials Credentials)> LoadAsync(
        ICommerceDbContext db,
        Guid integrationId,
        ILogger logger,
        CancellationToken cancellationToken)
    {
        var integration = await EnsureExistsAsync(db, integrationId, cancellationToken);

        if (!DataIngestCredentials.TryParse(integration.SettingsJson, out var credentials, out var reason))
        {
            logger.LogWarning(
                "Gravity integration credentials parse failed. IntegrationId={IntegrationId} Provider={Provider} Reason={Reason}",
                integration.Id,
                integration.Provider,
                reason);
            throw new InvalidOperationException(
                $"Integration does not have Gravity API credentials (url + apiKey + organizationId). {reason}");
        }

        logger.LogInformation(
            "Gravity integration ready. IntegrationId={IntegrationId} Provider={Provider} BaseUrl={BaseUrl} ApiKey={ApiKeyMask}",
            integration.Id,
            integration.Provider,
            credentials.BaseUrl,
            DataIngestCredentials.MaskApiKey(credentials.ApiKey));

        return (integration, credentials);
    }
}
