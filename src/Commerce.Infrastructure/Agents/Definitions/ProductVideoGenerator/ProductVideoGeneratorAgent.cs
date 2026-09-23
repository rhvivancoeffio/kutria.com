using System.Text.Json;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.DependencyInjection;
using Commerce.Application.Abstracts;
using Commerce.Infrastructure.Agents.Definitions.Generative;

namespace Commerce.Infrastructure.Agents.Definitions.ProductVideoGenerator;

public sealed class ProductVideoGeneratorAgent : IAgentModule
{
    public const string KeyName = "product-video-generator";

    public string Key => KeyName;

    public static IServiceCollection Add(IServiceCollection services)
        => services.AddScoped<IAgentModule, ProductVideoGeneratorAgent>();

    public IReadOnlyList<AITool> CreateTools(AgentDefinition definition, string tenantId, IServiceProvider services)
    {
        var available = new Dictionary<string, AITool>(StringComparer.OrdinalIgnoreCase)
        {
            [GenerateProductVideoTool.Name] = GenerateProductVideoTool.Create(services, tenantId)
        };
        return AgentTools.Bind(definition, available, services);
    }

    public Task ExecuteWorkflowAsync(
        AgentDefinition definition,
        string payload,
        IServiceProvider services,
        CancellationToken cancellationToken)
        => Task.CompletedTask;
}

internal static class GenerateProductVideoTool
{
    public const string Name = "generate_product_video";
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    public static AITool Create(IServiceProvider services, string tenantId)
        => AIFunctionFactory.Create(
            async (string name, string? description, string? image_url, CancellationToken cancellationToken) =>
            {
                _ = tenantId;
                _ = image_url;
                if (string.IsNullOrWhiteSpace(name))
                    return """{"error":"name is required"}""";

                var system = """
                    Eres guionista UGC para ecommerce Perú. Responde SOLO JSON:
                    {"jobId":"","status":"script_ready","script":"","hooks":[],"previewUrl":null}
                    script 60-90 palabras, hooks 2-4 frases cortas. Español.
                    """;
                var user =
                    $"Producto: {name.Trim()}\nDescripción: {(description ?? "").Trim()}\nGenera guion UGC stub (sin video real).";
                var raw = await GenerativeLlm.CompleteJsonAsync(services, system, user, cancellationToken);
                try
                {
                    using var doc = JsonDocument.Parse(raw);
                    var root = doc.RootElement;
                    if (root.TryGetProperty("error", out _))
                        return raw;

                    var jobId = root.TryGetProperty("jobId", out var j) && j.ValueKind == JsonValueKind.String
                        && !string.IsNullOrWhiteSpace(j.GetString())
                        ? j.GetString()!
                        : Guid.NewGuid().ToString("N");

                    var payload = new Dictionary<string, object?>
                    {
                        ["type"] = "message",
                        ["message"] = "Guion UGC listo (stub).",
                        ["video"] = new Dictionary<string, object?>
                        {
                            ["jobId"] = jobId,
                            ["status"] = root.TryGetProperty("status", out var st) ? st.GetString() ?? "script_ready" : "script_ready",
                            ["script"] = root.TryGetProperty("script", out var sc) ? sc.GetString() ?? "" : "",
                            ["hooks"] = root.TryGetProperty("hooks", out var hk)
                                ? JsonSerializer.Deserialize<object>(hk.GetRawText(), Json)
                                : Array.Empty<string>(),
                            ["previewUrl"] = null
                        }
                    };
                    return JsonSerializer.Serialize(payload, Json);
                }
                catch
                {
                    return JsonSerializer.Serialize(new
                    {
                        type = "message",
                        message = "Guion UGC stub.",
                        video = new
                        {
                            jobId = Guid.NewGuid().ToString("N"),
                            status = "script_ready",
                            script = raw,
                            hooks = Array.Empty<string>(),
                            previewUrl = (string?)null
                        }
                    }, Json);
                }
            },
            name: Name,
            description: AgentTools.Description(services, Name));
}
