using Commerce.Application.Common.Abstracts;

namespace Commerce.Application.Features.Catalog.Categories.ListCategories;

public sealed record ListCategoriesQuery(
    string? Name = null,
    Guid? IntegrationId = null) : IQuery<ListCategoriesResult>;
