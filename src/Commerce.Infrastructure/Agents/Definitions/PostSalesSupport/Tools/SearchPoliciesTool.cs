using System.Globalization;
using System.Text.Json;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.DependencyInjection;
using Commerce.Application.Abstracts;

namespace Commerce.Infrastructure.Agents.Definitions.PostSalesSupport.Tools;

internal static class SearchPoliciesTool
{
    public const string Name = "search_policies";
    public const float MinimumScore = 0.75f;

    public static AITool Create(IServiceProvider services, string tenantId)
    {
        var index = services.GetRequiredService<IBrainIndex>();
        var profiles = services.GetRequiredService<IBrainProfileRegistry>();
        var workspaceContext = services.GetRequiredService<IWorkspaceContext>();
        var workspaceId = workspaceContext.WorkspaceId
            ?? throw new InvalidOperationException("Workspace is required.");
        return AIFunctionFactory.Create(
            (string query, string? type, string? seller_id, string? purchase_date, CancellationToken cancellationToken) =>
                SearchAsync(index, profiles, tenantId, workspaceId, query, type, seller_id, purchase_date, cancellationToken),
            name: Name,
            description: AgentTools.Description(services, Name));
    }

    private static Task<string> SearchAsync(
        IBrainIndex index,
        IBrainProfileRegistry profiles,
        string tenantId,
        Guid workspaceId,
        string query,
        string? type,
        string? sellerId,
        string? purchaseDate,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(query))
        {
            return Task.FromResult(NotFound());
        }

        return SearchAsync(
            index, profiles, tenantId, workspaceId, query, type, sellerId, purchaseDate,
            evaluatedOnly: true, jobId: null, cancellationToken);
    }

    public static Task<string> SearchCandidateAsync(
        IBrainIndex index,
        IBrainProfileRegistry profiles,
        string tenantId,
        Guid? workspaceId,
        Guid jobId,
        string query,
        string? type,
        CancellationToken cancellationToken)
        => SearchAsync(
            index, profiles, tenantId, workspaceId, query, type, null, null,
            evaluatedOnly: false, jobId, cancellationToken);

    private static async Task<string> SearchAsync(
        IBrainIndex index,
        IBrainProfileRegistry profiles,
        string tenantId,
        Guid? workspaceId,
        string query,
        string? type,
        string? sellerId,
        string? purchaseDate,
        bool evaluatedOnly,
        Guid? jobId,
        CancellationToken cancellationToken)
    {
        var profile = profiles.Get(BrainKeys.Policy);
        var on = DateOnly.TryParse(purchaseDate, out var parsed) ? parsed : DateOnly.FromDateTime(DateTime.UtcNow);
        var must = new Dictionary<string, string> { ["language"] = "es-PE" };
        if (workspaceId is Guid ws)
        {
            must["workspace_id"] = ws.ToString("D");
        }

        if (evaluatedOnly)
        {
            must["is_evaluated"] = "true";
        }

        if (jobId is not null)
        {
            must["job_id"] = jobId.Value.ToString();
        }

        if (!string.IsNullOrWhiteSpace(type))
        {
            must["type"] = type.Trim().ToLowerInvariant();
        }

        var hits = await index.SearchAsync(
            new BrainSearch(profile.CollectionName, tenantId, query, 5, must),
            cancellationToken);
        var match = hits
            .Where(hit => Applies(hit.Payload, sellerId, on))
            .OrderByDescending(hit => hit.Score)
            .FirstOrDefault();
        if (match is null || match.Score <= MinimumScore)
        {
            return NotFound();
        }

        var content = Value(match.Payload, "content");
        var excerpt = content.Length <= 400 ? content : content[..400];
        return JsonSerializer.Serialize(new
        {
            found = true,
            policy_id = Value(match.Payload, "policy_id"),
            title = Value(match.Payload, "title"),
            excerpt,
            source_url = Value(match.Payload, "source_url"),
            effective_from = Value(match.Payload, "effective_from"),
            score = match.Score
        });
    }

    private static bool Applies(IReadOnlyDictionary<string, string> payload, string? sellerId, DateOnly on)
    {
        if (!string.IsNullOrWhiteSpace(sellerId))
        {
            var sellers = Value(payload, "applicable_sellers");
            if (!string.Equals(sellers, "all", StringComparison.OrdinalIgnoreCase)
                && !sellers.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                    .Contains(sellerId, StringComparer.OrdinalIgnoreCase))
            {
                return false;
            }
        }

        if (!DateOnly.TryParse(Value(payload, "effective_from"), CultureInfo.InvariantCulture, DateTimeStyles.None, out var from))
        {
            return false;
        }

        if (on < from)
        {
            return false;
        }

        var until = Value(payload, "effective_to");
        return string.IsNullOrWhiteSpace(until)
            || !DateOnly.TryParse(until, CultureInfo.InvariantCulture, DateTimeStyles.None, out var to)
            || on <= to;
    }

    private static string Value(IReadOnlyDictionary<string, string> payload, string key)
        => payload.TryGetValue(key, out var value) ? value : string.Empty;

    private static string NotFound()
        => JsonSerializer.Serialize(new
        {
            found = false,
            message = "No encuentro política vigente para eso, derivo a soporte humano."
        });
}
