using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Commerce.Application.Abstracts;
using Commerce.Domain.Policies;

namespace Commerce.Infrastructure.Policies;

public sealed class PolicyEvalCatalog(ICommerceDbContext db) : IPolicyEvalCatalog
{
    public IReadOnlyList<PolicyGoldenItem> LoadBase()
    {
        var document = LoadDocument();
        return document.Items;
    }

    public async Task<PolicyEvalSet> ResolveAsync(
        string? type = null,
        Guid? workspaceId = null,
        CancellationToken cancellationToken = default)
    {
        var document = LoadDocument();
        var query = db.PolicyEvalItems.AsNoTracking().AsQueryable();
        if (workspaceId is Guid ws)
            query = query.Where(x => x.WorkspaceId == ws);

        var overrides = await query.ToListAsync(cancellationToken);
        var byBase = overrides
            .Where(item => !string.IsNullOrWhiteSpace(item.BaseItemId))
            .GroupBy(item => item.BaseItemId!, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(group => group.Key, group => group.OrderByDescending(item => item.CreatedAt).First(), StringComparer.OrdinalIgnoreCase);

        var questions = new List<PolicyEvalQuestion>();
        foreach (var item in document.Items)
        {
            if (!Matches(item.Type, type))
            {
                continue;
            }

            if (!byBase.TryGetValue(item.Id, out var custom))
            {
                questions.Add(new PolicyEvalQuestion(item.Id, item.Type, item.Question, "base"));
                continue;
            }

            if (custom.IsDisabled)
            {
                continue;
            }

            questions.Add(new PolicyEvalQuestion(item.Id, item.Type, custom.Question, "override"));
        }

        foreach (var extra in overrides.Where(item => string.IsNullOrWhiteSpace(item.BaseItemId) && !item.IsDisabled && Matches(item.Type, type)).OrderBy(item => item.SortOrder))
        {
            questions.Add(new PolicyEvalQuestion(extra.Id.ToString(), extra.Type, extra.Question, "extra"));
        }

        return new PolicyEvalSet(document.Version, questions);
    }

    private static bool Matches(string? itemType, string? type)
    {
        if (type is null)
        {
            return true;
        }

        return string.Equals(itemType, type, StringComparison.OrdinalIgnoreCase);
    }

    private static GoldenDocument LoadDocument()
    {
        var path = Find();
        using var stream = File.OpenRead(path);
        var document = JsonSerializer.Deserialize<GoldenDocument>(stream, Json)
            ?? throw new InvalidOperationException("Policy golden template is empty.");
        return document;
    }

    private static string Find()
    {
        var current = new DirectoryInfo(AppContext.BaseDirectory);
        while (current is not null)
        {
            var candidate = Path.Combine(current.FullName, "data", "evals", "policy", "golden.json");
            if (File.Exists(candidate))
            {
                return candidate;
            }

            current = current.Parent;
        }

        throw new InvalidOperationException("Policy golden template was not found.");
    }

    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    private sealed record GoldenDocument(string Version, IReadOnlyList<PolicyGoldenItem> Items);
}
