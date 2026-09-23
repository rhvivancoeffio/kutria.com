using Commerce.Application.Common.Abstracts;

namespace Commerce.Application.Features.Catalog.Categories.GetCategory;

public sealed record GetCategoryQuery(string CategoryId, Guid? IntegrationId = null) : IQuery<GetCategoryResult?>;
