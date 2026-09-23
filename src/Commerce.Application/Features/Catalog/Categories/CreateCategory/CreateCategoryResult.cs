namespace Commerce.Application.Features.Catalog.Categories.CreateCategory;

public sealed record CreateCategoryResult(
    string CategoryId,
    string? Name,
    string? ParentId,
    string? Url);
