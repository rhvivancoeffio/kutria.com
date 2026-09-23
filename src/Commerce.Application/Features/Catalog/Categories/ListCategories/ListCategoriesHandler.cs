using Microsoft.Extensions.Logging;
using Commerce.Application.Abstracts;
using Commerce.Application.Common;
using Commerce.Application.Common.Abstracts;

namespace Commerce.Application.Features.Catalog.Categories.ListCategories;

public sealed class ListCategoriesHandler(
    ICommerceDbContext db,
    IWorkspaceContext workspaceContext,
    IGravityStoreDataClient gravityStore,
    ILogger<ListCategoriesHandler> logger)
    : IQueryHandler<ListCategoriesQuery, ListCategoriesResult>
{
    public async Task<ListCategoriesResult> Handle(ListCategoriesQuery request, CancellationToken cancellationToken)
    {
        var workspaceId = await WorkspaceRequirement.RequireWorkspaceIdAsync(
            workspaceContext, db, cancellationToken);
        var (_, credentials) = await CatalogGravityCredentials.LoadActiveAsync(
            db,
            logger,
            workspaceId,
            request.IntegrationId,
            cancellationToken);

        var flat = await gravityStore.ListCategoriesAsync(credentials, request.Name, cancellationToken);
        return new ListCategoriesResult(BuildTree(flat));
    }

    internal static IReadOnlyList<CategoryTreeNode> BuildTree(IReadOnlyList<GravityCategoryItem> flat)
    {
        var items = flat
            .Where(x => !string.IsNullOrWhiteSpace(x.CategoryId))
            .ToList();
        var knownIds = new HashSet<string>(items.Select(x => x.CategoryId), StringComparer.OrdinalIgnoreCase);
        var childrenByParent = new Dictionary<string, List<GravityCategoryItem>>(StringComparer.OrdinalIgnoreCase);
        var roots = new List<GravityCategoryItem>();

        foreach (var item in items)
        {
            var parent = string.IsNullOrWhiteSpace(item.ParentId) ? null : item.ParentId.Trim();
            if (parent is null || !knownIds.Contains(parent))
            {
                roots.Add(item);
                continue;
            }

            if (!childrenByParent.TryGetValue(parent, out var list))
            {
                list = [];
                childrenByParent[parent] = list;
            }
            list.Add(item);
        }

        CategoryTreeNode Map(GravityCategoryItem item)
        {
            childrenByParent.TryGetValue(item.CategoryId, out var kids);
            var children = (kids ?? [])
                .OrderBy(c => c.Name, StringComparer.OrdinalIgnoreCase)
                .Select(Map)
                .ToList();
            return new CategoryTreeNode(
                item.CategoryId,
                item.Name,
                string.IsNullOrWhiteSpace(item.ParentId) ? null : item.ParentId,
                item.Url,
                children);
        }

        return roots
            .OrderBy(r => r.Name, StringComparer.OrdinalIgnoreCase)
            .Select(Map)
            .ToList();
    }
}
