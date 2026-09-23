namespace Commerce.Application.Features.Catalog.Categories.GetCategory;

public sealed record GetCategoryResult(
    string CategoryId,
    string? Name,
    string? ParentId,
    string? Url);
