using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Commerce.Application.Abstracts;
using Commerce.Application.Common;
using Commerce.Application.Common.Abstracts;
using Commerce.Application.Features.DataIngestion;

namespace Commerce.Application.Features.Integrations.UpdateIntegration;

public sealed class UpdateIntegrationHandler(
    ICommerceDbContext db,
    IWorkspaceContext workspaceContext,
    IIntegrationsMetadataService metadataService,
    IGravityStoreDataClient gravityStore,
    ILogger<UpdateIntegrationHandler> logger)
    : ICommandHandler<UpdateIntegrationCommand, UpdateIntegrationResult>
{
    private static readonly HashSet<string> SensitiveKeys = new(StringComparer.OrdinalIgnoreCase)
    {
        "appKey", "appToken", "clientSecret", "accessToken", "consumerKey", "consumerSecret",
        "apiKey", "bearerToken", "oauthAccessToken", "oauthRefreshToken", "oauthClientSecret", "personalApiToken",
        MeetGravitySettingKeys.ClientSecret,
        MeetGravitySettingKeys.AccessToken
    };

    public async Task<UpdateIntegrationResult> Handle(
        UpdateIntegrationCommand request,
        CancellationToken cancellationToken)
    {
        var workspaceId = await WorkspaceRequirement.RequireWorkspaceIdAsync(
            workspaceContext, db, cancellationToken);

        var integration = await db.Integrations
                .FirstOrDefaultAsync(i => i.Id == request.Id && i.WorkspaceId == workspaceId, cancellationToken)
            ?? throw new KeyNotFoundException($"Integration {request.Id} was not found.");

        if (!string.IsNullOrWhiteSpace(request.Name))
            integration.Name = request.Name.Trim();

        if (request.IsActive.HasValue)
            integration.IsActive = request.IsActive.Value;

        if (request.Settings != null)
        {
            var meta = await metadataService.GetByProviderAsync(integration.Provider, cancellationToken);
            var incoming = IntegrationSettingsPayloadNormalizer.ToStringDictionary(meta, request.Settings);

            Dictionary<string, string> existing;
            try
            {
                existing = string.IsNullOrWhiteSpace(integration.SettingsJson)
                    ? new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
                    : JsonSerializer.Deserialize<Dictionary<string, string>>(integration.SettingsJson)
                      ?? new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            }
            catch (JsonException)
            {
                existing = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            }

            foreach (var kv in incoming)
            {
                if (SensitiveKeys.Contains(kv.Key)
                    && string.Equals(kv.Value, "***", StringComparison.Ordinal)
                    && existing.TryGetValue(kv.Key, out var prev)
                    && !string.IsNullOrEmpty(prev))
                {
                    continue;
                }

                existing[kv.Key] = kv.Value;
            }

            if (GravityDefaultSellerBootstrap.IsGravityProvider(integration.Provider))
            {
                await GravityDefaultSellerBootstrap.EnrichSettingsWithDefaultSellerAsync(
                        existing,
                        gravityStore,
                        logger,
                        cancellationToken)
                    .ConfigureAwait(false);
            }

            integration.SettingsJson = JsonSerializer.Serialize(existing);
        }
        else if (GravityDefaultSellerBootstrap.IsGravityProvider(integration.Provider)
                 && integration.IsActive
                 && !HasSellerId(integration.SettingsJson))
        {
            var map = DeserializeSettings(integration.SettingsJson);
            await GravityDefaultSellerBootstrap.EnrichSettingsWithDefaultSellerAsync(
                    map,
                    gravityStore,
                    logger,
                    cancellationToken)
                .ConfigureAwait(false);
            integration.SettingsJson = JsonSerializer.Serialize(map);
        }

        await db.SaveChangesAsync(cancellationToken);

        return new UpdateIntegrationResult(
            integration.Id,
            integration.WorkspaceId,
            integration.Provider,
            integration.Name,
            integration.IsActive);
    }

    private static bool HasSellerId(string? settingsJson)
    {
        var map = DeserializeSettings(settingsJson);
        return map.TryGetValue(DataIngestCredentials.SellerIdSettingKey, out var id)
               && !string.IsNullOrWhiteSpace(id);
    }

    private static Dictionary<string, string> DeserializeSettings(string? settingsJson)
    {
        try
        {
            return string.IsNullOrWhiteSpace(settingsJson)
                ? new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
                : JsonSerializer.Deserialize<Dictionary<string, string>>(settingsJson)
                  ?? new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        }
        catch (JsonException)
        {
            return new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        }
    }
}
