namespace Commerce.Application.Features.Catalog.Brands.GetBrand;

public sealed record GetBrandResult(string BrandId, string? Name, bool IsActive);
