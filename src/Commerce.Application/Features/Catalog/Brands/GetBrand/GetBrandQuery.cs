using Commerce.Application.Common.Abstracts;

namespace Commerce.Application.Features.Catalog.Brands.GetBrand;

public sealed record GetBrandQuery(string BrandId, Guid? IntegrationId = null) : IQuery<GetBrandResult?>;
