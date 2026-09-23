namespace Commerce.Application.Abstracts;

/// <summary>
/// Provides metadata of available e-commerce integrations (VTEX, Shopify, etc.).
/// Data is loaded from YAML under data/integrations.
/// </summary>
public interface IIntegrationsMetadataService
{
    Task<IReadOnlyList<IntegrationMetadataDto>> GetAvailableIntegrationsAsync(
        CancellationToken cancellationToken = default);

    Task<IntegrationMetadataDto?> GetByProviderAsync(
        string provider,
        CancellationToken cancellationToken = default);
}

public sealed class IntegrationMetadataDto
{
    public string Key { get; set; } = string.Empty;
    public string Type { get; set; } = "api";
    public string? IntegrationType { get; set; }
    public bool ChannelMarketplace { get; set; }
    public bool PlanLimitCountsAsStore { get; set; }
    public string? MarketplaceKey { get; set; }
    public bool AvailableForConnect { get; set; } = true;
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string? LogoUrl { get; set; }
    public string? DocumentationUrl { get; set; }
    public string? AuthType { get; set; }
    public string? OAuthCallbackPath { get; set; }
    public string? CredentialsSource { get; set; }
    public bool HasCustomHeaders { get; set; }
    public List<IntegrationSettingSchemaDto> Settings { get; set; } = [];
    public List<IntegrationSettingsGroupSchemaDto> SettingsGroups { get; set; } = [];
}

public sealed class IntegrationSettingsGroupSchemaDto
{
    public string Key { get; set; } = string.Empty;
    public string? Label { get; set; }
    public string? Description { get; set; }
}

public sealed class IntegrationSettingSchemaDto
{
    public string Key { get; set; } = string.Empty;
    public string Label { get; set; } = string.Empty;
    public string Type { get; set; } = "text";
    public bool Required { get; set; }
    public string? Placeholder { get; set; }
    public string? Help { get; set; }
    public string? Default { get; set; }
    public bool Readonly { get; set; }
    public List<IntegrationSettingOptionDto> Options { get; set; } = [];
    public Dictionary<string, string>? VisibleWhen { get; set; }
    public string? Group { get; set; }
}

public sealed class IntegrationSettingOptionDto
{
    public string Value { get; set; } = string.Empty;
    public string Label { get; set; } = string.Empty;
}
