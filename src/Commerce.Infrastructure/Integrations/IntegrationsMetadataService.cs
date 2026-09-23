using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Commerce.Application.Abstracts;

namespace Commerce.Infrastructure.Integrations;

/// <summary>
/// Loads <c>data/integrations/*.integration.yaml</c> via <see cref="IYamlMetadataService"/>
/// and maps YAML models to <see cref="IntegrationMetadataDto"/>.
/// </summary>
public sealed class IntegrationsMetadataService : IIntegrationsMetadataService
{
    private const string CacheKeyPrefix = "Integrations:Available:v1:";
    private static readonly TimeSpan CacheExpiration = TimeSpan.FromHours(1);

    private readonly IMemoryCache _cache;
    private readonly ILogger<IntegrationsMetadataService> _logger;
    private readonly IYamlMetadataService _yaml;

    public IntegrationsMetadataService(
        IMemoryCache cache,
        ILogger<IntegrationsMetadataService> logger,
        IYamlMetadataService yaml)
    {
        _cache = cache;
        _logger = logger;
        _yaml = yaml;
    }

    public Task<IReadOnlyList<IntegrationMetadataDto>> GetAvailableIntegrationsAsync(
        CancellationToken cancellationToken = default)
    {
        var dataRoot = _yaml.GetDataRootPath();
        var integrationsDir = Path.Combine(dataRoot, "integrations");
        var cacheKey = CacheKeyPrefix + BuildIntegrationsDirFingerprint(integrationsDir);

        return _cache.GetOrCreateAsync(cacheKey, async entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = CacheExpiration;

            var result = new List<IntegrationMetadataDto>();
            if (!Directory.Exists(integrationsDir))
            {
                _logger.LogWarning("Integrations directory not found: {Path}", integrationsDir);
                return (IReadOnlyList<IntegrationMetadataDto>)result;
            }

            var files = Directory.GetFiles(integrationsDir, "*.integration.yaml", SearchOption.TopDirectoryOnly);
            _logger.LogInformation(
                "Loading integration metadata from {Dir} ({FileCount} files, dataRoot={DataRoot})",
                integrationsDir,
                files.Length,
                dataRoot);

            foreach (var filePath in files)
            {
                try
                {
                    var yamlContent = await File.ReadAllTextAsync(filePath, cancellationToken);
                    var root = _yaml.DeserializeRequiredFromYamlDocument<IntegrationYamlRoot>(yamlContent);
                    var meta = root.Integration;
                    if (meta == null || string.IsNullOrEmpty(meta.Key))
                    {
                        _logger.LogWarning(
                            "Skipping integration file {File}: missing integration.key",
                            Path.GetFileName(filePath));
                        continue;
                    }

                    var isChannelMarketplace = meta.ChannelMarketplace;
                    var marketplaceKey = string.IsNullOrWhiteSpace(meta.MarketplaceKey)
                        ? meta.Key.Trim().ToLowerInvariant()
                        : meta.MarketplaceKey.Trim().ToLowerInvariant();
                    var hasMarketplaceKey = isChannelMarketplace || !string.IsNullOrWhiteSpace(meta.MarketplaceKey);

                    result.Add(new IntegrationMetadataDto
                    {
                        Key = meta.Key,
                        Type = meta.Type ?? "api",
                        IntegrationType = meta.IntegrationType,
                        ChannelMarketplace = isChannelMarketplace,
                        PlanLimitCountsAsStore = meta.PlanLimitCountsAsStore,
                        MarketplaceKey = hasMarketplaceKey ? marketplaceKey : null,
                        AvailableForConnect = meta.IsActive,
                        Name = meta.Name ?? meta.Key,
                        Description = meta.Description ?? string.Empty,
                        LogoUrl = meta.LogoUrl,
                        DocumentationUrl = meta.DocumentationUrl,
                        AuthType = meta.AuthType,
                        OAuthCallbackPath = meta.OAuthCallbackPath,
                        CredentialsSource = meta.CredentialsSource,
                        HasCustomHeaders = meta.HasCustomHeaders,
                        SettingsGroups = MapSettingsGroups(meta.SettingsGroups),
                        Settings = (meta.Settings ?? [])
                            .Where(s => s != null)
                            .Select(s => new IntegrationSettingSchemaDto
                            {
                                Key = s!.Key ?? string.Empty,
                                Label = s.Label ?? string.Empty,
                                Type = s.Type ?? "text",
                                Required = s.Required,
                                Placeholder = s.Placeholder,
                                Help = s.Help,
                                Default = s.Default,
                                Readonly = s.Readonly,
                                Options = (s.Options ?? [])
                                    .Where(o => o != null)
                                    .Select(o => new IntegrationSettingOptionDto
                                    {
                                        Value = o!.Value ?? o.Label ?? string.Empty,
                                        Label = o.Label ?? o.Value ?? string.Empty
                                    })
                                    .Where(o => !string.IsNullOrEmpty(o.Value))
                                    .ToList(),
                                VisibleWhen = s.VisibleWhen,
                                Group = string.IsNullOrWhiteSpace(s.Group) ? null : s.Group.Trim()
                            })
                            .ToList()
                    });
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to load integration from {File}", Path.GetFileName(filePath));
                }
            }

            _logger.LogInformation("Loaded {Count} integration metadata entries from YAML", result.Count);
            return (IReadOnlyList<IntegrationMetadataDto>)result;
        })!;
    }

    public async Task<IntegrationMetadataDto?> GetByProviderAsync(
        string provider,
        CancellationToken cancellationToken = default)
    {
        var all = await GetAvailableIntegrationsAsync(cancellationToken);
        return all.FirstOrDefault(m => string.Equals(m.Key, provider, StringComparison.OrdinalIgnoreCase));
    }

    private static string BuildIntegrationsDirFingerprint(string integrationsDir)
    {
        if (!Directory.Exists(integrationsDir))
            return "missing";

        var parts = Directory
            .GetFiles(integrationsDir, "*.integration.yaml", SearchOption.TopDirectoryOnly)
            .Select(f =>
            {
                var info = new FileInfo(f);
                return $"{info.Name}:{info.Length}:{info.LastWriteTimeUtc.Ticks}";
            })
            .OrderBy(p => p, StringComparer.Ordinal)
            .ToList();

        return parts.Count == 0 ? "empty" : string.Join("|", parts);
    }

    private static List<IntegrationSettingsGroupSchemaDto> MapSettingsGroups(
        Dictionary<string, IntegrationSettingsGroupYaml>? groups)
    {
        if (groups == null || groups.Count == 0)
            return [];

        var list = new List<IntegrationSettingsGroupSchemaDto>();
        foreach (var kv in groups.OrderBy(k => k.Key, StringComparer.OrdinalIgnoreCase))
        {
            var key = kv.Key?.Trim();
            if (string.IsNullOrEmpty(key) || kv.Value == null)
                continue;
            var g = kv.Value;
            list.Add(new IntegrationSettingsGroupSchemaDto
            {
                Key = key,
                Label = string.IsNullOrWhiteSpace(g.Label) ? null : g.Label.Trim(),
                Description = string.IsNullOrWhiteSpace(g.Description) ? null : g.Description.Trim()
            });
        }

        return list;
    }

    private sealed class IntegrationYamlRoot
    {
        public IntegrationMetaYaml? Integration { get; set; }
    }

    private sealed class IntegrationMetaYaml
    {
        public string? Key { get; set; }
        public string? Type { get; set; }
        public string? IntegrationType { get; set; }
        public bool ChannelMarketplace { get; set; }
        public bool PlanLimitCountsAsStore { get; set; }
        public string? MarketplaceKey { get; set; }
        public string? Name { get; set; }
        public string? Description { get; set; }
        public string? LogoUrl { get; set; }
        public string? DocumentationUrl { get; set; }
        public string? AuthType { get; set; }
        public bool IsActive { get; set; } = true;
        public string? OAuthCallbackPath { get; set; }
        public string? CredentialsSource { get; set; }
        public bool HasCustomHeaders { get; set; }
        public List<IntegrationSettingYaml>? Settings { get; set; }
        public Dictionary<string, IntegrationSettingsGroupYaml>? SettingsGroups { get; set; }
    }

    private sealed class IntegrationSettingsGroupYaml
    {
        public string? Label { get; set; }
        public string? Description { get; set; }
    }

    private sealed class IntegrationSettingYaml
    {
        public string? Key { get; set; }
        public string? Label { get; set; }
        public string? Type { get; set; }
        public bool Required { get; set; }
        public string? Placeholder { get; set; }
        public string? Help { get; set; }
        public string? Default { get; set; }
        public bool Readonly { get; set; }
        public string? Group { get; set; }
        public List<IntegrationSettingOptionYaml>? Options { get; set; }
        public Dictionary<string, string>? VisibleWhen { get; set; }
    }

    private sealed class IntegrationSettingOptionYaml
    {
        public string? Value { get; set; }
        public string? Label { get; set; }
    }
}
