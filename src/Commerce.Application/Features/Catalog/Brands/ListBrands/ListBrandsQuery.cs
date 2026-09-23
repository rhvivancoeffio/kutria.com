using Commerce.Application.Common.Abstracts;

namespace Commerce.Application.Features.Catalog.Brands.ListBrands;

public sealed record ListBrandsQuery(
    string? Name = null,
    int Page = 1,
    int PageSize = 20,
    Guid? IntegrationId = null) : IQuery<ListBrandsResult>;
