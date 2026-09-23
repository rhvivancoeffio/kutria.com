using System.Text.Json;
using Commerce.Application.Abstracts;
using Commerce.Application.Features.DataIngestion;
using Microsoft.Extensions.Logging;

namespace Commerce.Application.Features.Integrations;

/// <summary>
/// On Gravity connect/update: GET /sellers/default and persist sellerId into SettingsJson.
/// </summary>
public static class GravityDefaultSellerBootstrap
{
    public static bool IsGravityProvider(string? provider)
        => string.Equals(provider, "Gravity", StringComparison.OrdinalIgnoreCase)
           || string.Equals(provider, "GravityAPI", StringComparison.OrdinalIgnoreCase);

    public static async Task EnrichSettingsWithDefaultSellerAsync(
        IDictionary<string, string> settings,
        IGravityStoreDataClient gravity,
        ILogger logger,
        CancellationToken cancellationToken)
    {
        var json = JsonSerializer.Serialize(settings);
        if (!DataIngestCredentials.TryParse(json, out var credentials, out var reason))
        {
            throw new InvalidOperationException(
                $"Cannot resolve Gravity default seller: credentials incomplete ({reason}).");
        }

        var seller = await gravity.GetDefaultSellerAsync(credentials, cancellationToken)
            .ConfigureAwait(false);

        settings[DataIngestCredentials.SellerIdSettingKey] = seller.SellerId;
        if (!string.IsNullOrWhiteSpace(seller.Name))
            settings[DataIngestCredentials.SellerNameSettingKey] = seller.Name;

        logger.LogInformation(
            "Gravity default seller resolved. SellerId={SellerId} Name={Name}",
            seller.SellerId,
            seller.Name ?? "(none)");
    }
}
