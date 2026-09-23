using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;
using Commerce.Application.Abstracts;
using Commerce.Infrastructure.Agents.Metadata;

namespace Commerce.Infrastructure.Yaml;

/// <summary>
/// Generic YAML I/O for the host <c>data/</c> tree. Does not own domain DTO mapping.
/// </summary>
public sealed class YamlMetadataService : IYamlMetadataService
{
    private readonly IDeserializer _hostDeserializer = new DeserializerBuilder()
        .WithNamingConvention(CamelCaseNamingConvention.Instance)
        .IgnoreUnmatchedProperties()
        .Build();

    private readonly IDeserializer _agentDeserializer = new DeserializerBuilder()
        .WithNamingConvention(UnderscoredNamingConvention.Instance)
        .WithTypeConverter(new AgentYamlModelConverter())
        .WithTypeConverter(new AgentYamlToolConverter())
        .IgnoreUnmatchedProperties()
        .Build();

    public string GetDataRootPath() => YamlDataPaths.ResolveDataRootDirectory();

    public async Task<T?> DeserializeAsync<T>(string relativePathUnderData, CancellationToken cancellationToken = default)
        where T : class
    {
        var path = YamlDataPaths.TryResolveExistingFilePath(relativePathUnderData);
        if (path is null)
        {
            return null;
        }

        var content = await File.ReadAllTextAsync(path, cancellationToken);
        return _hostDeserializer.Deserialize<T>(content);
    }

    public async Task<T> DeserializeRequiredAsync<T>(string relativePathUnderData, CancellationToken cancellationToken = default)
        where T : class
    {
        var path = YamlDataPaths.TryResolveExistingFilePath(relativePathUnderData)
            ?? throw new FileNotFoundException(
                $"YAML not found: {relativePathUnderData}",
                Path.Combine(GetDataRootPath(), YamlDataPaths.Normalize(relativePathUnderData)));

        var content = await File.ReadAllTextAsync(path, cancellationToken);
        return DeserializeRequiredFromYamlDocument<T>(content);
    }

    public T DeserializeRequiredFromYamlDocument<T>(string yamlContent)
        where T : class
    {
        var result = _hostDeserializer.Deserialize<T>(yamlContent);
        return result ?? throw new InvalidOperationException("YAML document deserialized to null.");
    }

    public T DeserializeAgentYamlDocument<T>(string yamlContent)
        where T : class
    {
        var result = _agentDeserializer.Deserialize<T>(yamlContent);
        return result ?? throw new InvalidOperationException("Agent YAML document deserialized to null.");
    }
}
