using YamlDotNet.Core;
using YamlDotNet.Core.Events;
using YamlDotNet.Serialization;

namespace Commerce.Infrastructure.Agents.Metadata;

/// <summary>
/// Accepts <c>model: gpt-4o-mini</c> and the object form used by checkout.
/// </summary>
internal sealed class AgentYamlModelConverter : IYamlTypeConverter
{
    public bool Accepts(Type type) => type == typeof(AgentYamlModel);

    public object? ReadYaml(IParser parser, Type type, ObjectDeserializer rootDeserializer)
    {
        if (parser.Current is Scalar scalar)
        {
            parser.MoveNext();
            return new AgentYamlModel { Name = scalar.Value };
        }

        var body = (AgentYamlModelBody)rootDeserializer(typeof(AgentYamlModelBody))!;
        return new AgentYamlModel
        {
            Name = body.Name,
            Temperature = body.Temperature,
            MaxTokens = body.MaxTokens
        };
    }

    public void WriteYaml(IEmitter emitter, object? value, Type type, ObjectSerializer serializer)
        => throw new NotSupportedException("Agent YAML is read-only.");
}

/// <summary>
/// Accepts <c>- get_cart</c> and <c>- name: get_cart</c>.
/// </summary>
internal sealed class AgentYamlToolConverter : IYamlTypeConverter
{
    public bool Accepts(Type type) => type == typeof(AgentYamlTool);

    public object? ReadYaml(IParser parser, Type type, ObjectDeserializer rootDeserializer)
    {
        if (parser.Current is Scalar scalar)
        {
            parser.MoveNext();
            return new AgentYamlTool { Name = scalar.Value };
        }

        var body = (AgentYamlToolBody)rootDeserializer(typeof(AgentYamlToolBody))!;
        return new AgentYamlTool
        {
            Name = body.Name,
            TimeoutMs = body.TimeoutMs,
            SideEffect = body.SideEffect,
            Requires = body.Requires
        };
    }

    public void WriteYaml(IEmitter emitter, object? value, Type type, ObjectSerializer serializer)
        => throw new NotSupportedException("Agent YAML is read-only.");
}

internal sealed class AgentYamlModelBody
{
    public string? Name { get; set; }
    public double? Temperature { get; set; }
    public int? MaxTokens { get; set; }
}

internal sealed class AgentYamlToolBody
{
    public string? Name { get; set; }
    public int? TimeoutMs { get; set; }
    public bool? SideEffect { get; set; }
    public List<string>? Requires { get; set; }
}
