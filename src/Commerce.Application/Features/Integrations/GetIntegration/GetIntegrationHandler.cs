using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Commerce.Application.Abstracts;
using Commerce.Application.Common;
using Commerce.Application.Common.Abstracts;

namespace Commerce.Application.Features.Integrations.GetIntegration;

public sealed class GetIntegrationHandler(
    ICommerceDbContext db,
    IWorkspaceContext workspaceContext,
    IIntegrationsMetadataService metadataService)
    : IQueryHandler<GetIntegrationQuery, GetIntegrationResult?>
{
    private static readonly HashSet<string> SensitiveKeys = new(StringComparer.OrdinalIgnoreCase)
    {
        "appKey", "appToken", "clientSecret", "accessToken", "consumerKey", "consumerSecret",
        "apiKey", "bearerToken", "oauthAccessToken", "oauthRefreshToken", "oauthClientSecret", "personalApiToken",
        MeetGravitySettingKeys.ClientSecret,
        MeetGravitySettingKeys.AccessToken
    };

    public async Task<GetIntegrationResult?> Handle(
        GetIntegrationQuery request,
        CancellationToken cancellationToken)
    {
        var workspaceId = await WorkspaceRequirement.RequireWorkspaceIdAsync(
            workspaceContext, db, cancellationToken);

        var integration = await db.Integrations
            .Where(i => i.Id == request.Id && i.WorkspaceId == workspaceId)
            .Select(i => new
            {
                i.Id,
                i.WorkspaceId,
                i.Provider,
                i.Name,
                i.SettingsJson,
                i.IsActive,
                i.CreatedAt
            })
            .FirstOrDefaultAsync(cancellationToken);

        if (integration is null)
            return null;

        Dictionary<string, string> settings;
        try
        {
            settings = string.IsNullOrWhiteSpace(integration.SettingsJson)
                ? new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
                : JsonSerializer.Deserialize<Dictionary<string, string>>(integration.SettingsJson)
                  ?? new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        }
        catch (JsonException)
        {
            settings = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        }

        if (!request.RevealSensitiveSettings)
        {
            var meta = await metadataService.GetByProviderAsync(integration.Provider, cancellationToken);
            settings = MaskSensitiveSettings(settings, meta);
        }

        return new GetIntegrationResult(
            integration.Id,
            integration.WorkspaceId,
            integration.Provider,
            integration.Name,
            settings,
            integration.IsActive,
            integration.CreatedAt);
    }

    private static Dictionary<string, string> MaskSensitiveSettings(
        Dictionary<string, string> settings,
        IntegrationMetadataDto? meta)
    {
        var passwordKeys = new HashSet<string>(SensitiveKeys, StringComparer.OrdinalIgnoreCase);
        if (meta?.Settings is { Count: > 0 })
        {
            foreach (var s in meta.Settings)
            {
                if (!string.IsNullOrEmpty(s.Key)
                    && string.Equals(s.Type, "password", StringComparison.OrdinalIgnoreCase))
                {
                    passwordKeys.Add(s.Key);
                }
            }
        }

        var masked = new Dictionary<string, string>(settings, StringComparer.OrdinalIgnoreCase);
        foreach (var key in settings.Keys.ToList())
        {
            if (passwordKeys.Contains(key) && !string.IsNullOrEmpty(masked[key]))
                masked[key] = "***";
        }

        return masked;
    }
}
