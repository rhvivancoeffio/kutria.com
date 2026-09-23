using Commerce.Application.Common.Abstracts;

namespace Commerce.Application.Features.Catalog.Categories.CreateCategory;

public sealed record CreateCategoryCommand(
    string Name,
    string? Slug = null,
    string? Description = null,
    string? ParentCategoryId = null,
    bool IsActive = true,
    Guid? IntegrationId = null) : ICommand<CreateCategoryResult>;
