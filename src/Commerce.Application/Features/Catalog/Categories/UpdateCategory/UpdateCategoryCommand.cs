using Commerce.Application.Common.Abstracts;

namespace Commerce.Application.Features.Catalog.Categories.UpdateCategory;

public sealed record UpdateCategoryCommand(
    string CategoryId,
    string Name,
    string? Slug = null,
    string? Description = null,
    string? ParentCategoryId = null,
    bool IsActive = true,
    Guid? IntegrationId = null) : ICommand<UpdateCategoryResult>;
