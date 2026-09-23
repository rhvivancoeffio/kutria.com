using Commerce.Application.Common.Abstracts;

namespace Commerce.Application.Features.Catalog.Brands.DeleteBrand;

public sealed record DeleteBrandCommand(string BrandId, Guid? IntegrationId = null) : ICommand<DeleteBrandResult>;
