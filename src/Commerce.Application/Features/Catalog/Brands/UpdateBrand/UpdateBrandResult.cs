namespace Commerce.Application.Features.Catalog.Brands.UpdateBrand;

public sealed record UpdateBrandResult(string BrandId, string? Name, bool IsActive);
