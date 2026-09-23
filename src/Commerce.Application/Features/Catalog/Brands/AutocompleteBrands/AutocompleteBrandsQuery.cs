using Commerce.Application.Common.Abstracts;

namespace Commerce.Application.Features.Catalog.Brands.AutocompleteBrands;

public sealed record AutocompleteBrandsQuery(string Name, Guid? IntegrationId = null)
    : IQuery<AutocompleteBrandsResult>;
