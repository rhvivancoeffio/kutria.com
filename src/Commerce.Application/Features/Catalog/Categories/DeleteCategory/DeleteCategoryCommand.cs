using Commerce.Application.Common.Abstracts;

namespace Commerce.Application.Features.Catalog.Categories.DeleteCategory;

public sealed record DeleteCategoryCommand(string CategoryId, Guid? IntegrationId = null)
    : ICommand<DeleteCategoryResult>;
