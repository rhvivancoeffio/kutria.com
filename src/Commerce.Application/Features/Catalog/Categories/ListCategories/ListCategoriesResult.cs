namespace Commerce.Application.Features.Catalog.Categories.ListCategories;

public sealed record ListCategoriesResult(IReadOnlyList<CategoryTreeNode> Items);

/// <summary>Flat-friendly node; children form the tree for UI.</summary>
public sealed record CategoryTreeNode(
    string CategoryId,
    string? Name,
    string? ParentId,
    string? Url,
    IReadOnlyList<CategoryTreeNode> Children);
