namespace Commerce.Application.Features.Catalog.Categories.UpdateCategory;

public sealed record UpdateCategoryResult(
    string CategoryId,
    string? Name,
    string? ParentId,
    string? Url);
