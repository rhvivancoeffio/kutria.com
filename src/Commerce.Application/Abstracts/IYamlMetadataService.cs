namespace Commerce.Application.Abstracts;

/// <summary>
/// Resolves the on-disk <c>data/</c> tree and deserializes YAML.
/// Host/catalog YAML (integrations, etc.) uses CamelCase; agents use a dedicated underscored profile.
/// Domain mapping belongs in feature services (e.g. <c>IntegrationsMetadataService</c>), not here.
/// </summary>
public interface IYamlMetadataService
{
    string GetDataRootPath();

    Task<T?> DeserializeAsync<T>(string relativePathUnderData, CancellationToken cancellationToken = default)
        where T : class;

    Task<T> DeserializeRequiredAsync<T>(string relativePathUnderData, CancellationToken cancellationToken = default)
        where T : class;

    /// <summary>CamelCase YamlDotNet — integrations and other host catalog YAML.</summary>
    T DeserializeRequiredFromYamlDocument<T>(string yamlContent)
        where T : class;

    /// <summary>Underscored + agent converters — <c>data/agents/**</c> documents only.</summary>
    T DeserializeAgentYamlDocument<T>(string yamlContent)
        where T : class;
}
