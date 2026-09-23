using System.Text.Json;
using Commerce.Application.Abstracts;

namespace Commerce.Infrastructure.Policies;

public sealed class PolicyBrainProfile : IBrainProfile
{
    public const string Collection = "policy-brain";

    public string Key => BrainKeys.Policy;

    public string CollectionName => Collection;

    public IReadOnlyList<BrainPoint> BuildPoints(BrainPublishedDocument document)
    {
        using var metadata = JsonDocument.Parse(string.IsNullOrWhiteSpace(document.MetadataJson) ? "{}" : document.MetadataJson);
        var type = Read(metadata, "type");
        var language = Read(metadata, "language");
        if (string.IsNullOrWhiteSpace(language))
        {
            language = "es-PE";
        }

        var effectiveFrom = Read(metadata, "effective_from");
        var policyId = Read(metadata, "policy_id");
        if (string.IsNullOrWhiteSpace(policyId))
        {
            policyId = $"{type}_{effectiveFrom}";
        }

        const string chunk = "1";
        var pointId = string.IsNullOrWhiteSpace(document.WorkspaceId?.ToString())
            ? $"{document.TenantId}|{policyId}|{chunk}|{language}|{effectiveFrom}"
            : $"{document.TenantId}|{document.WorkspaceId:N}|{policyId}|{chunk}|{language}|{effectiveFrom}";
        var title = Read(metadata, "title");
        if (string.IsNullOrWhiteSpace(title))
        {
            title = Path.GetFileNameWithoutExtension(document.FileName);
        }

        var payload = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["tenant_id"] = document.TenantId,
            ["workspace_id"] = document.WorkspaceId?.ToString("D") ?? string.Empty,
            ["policy_id"] = policyId,
            ["chunk_id"] = chunk,
            ["type"] = type,
            ["title"] = title,
            ["content"] = document.Text,
            ["content_enriched"] = document.Text,
            ["applicable_sellers"] = string.IsNullOrWhiteSpace(Read(metadata, "applicable_sellers"))
                ? "all"
                : Read(metadata, "applicable_sellers"),
            ["effective_from"] = effectiveFrom,
            ["effective_to"] = Read(metadata, "effective_to"),
            ["source_url"] = document.SourceUrl,
            ["language"] = language,
            ["point_key"] = pointId,
            ["job_id"] = document.JobId.ToString(),
            ["is_evaluated"] = "false"
        };
        return [new BrainPoint(pointId, document.Text, payload)];
    }

    private static string Read(JsonDocument document, string name)
        => document.RootElement.TryGetProperty(name, out var value) ? value.ToString() : string.Empty;
}
