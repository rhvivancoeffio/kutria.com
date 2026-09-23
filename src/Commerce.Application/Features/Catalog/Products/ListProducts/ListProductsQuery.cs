using Commerce.Application.Common.Abstracts;

namespace Commerce.Application.Features.Catalog.Products.ListProducts;

public sealed record ListProductsQuery(
    int Page = 1,
    int PageSize = 20,
    Guid? IntegrationId = null) : IQuery<ListProductsResult>;
