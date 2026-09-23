namespace Commerce.Application.Features.Catalog.Brands.ListBrands;

public sealed record ListBrandsResult(IReadOnlyList<BrandListItem> Items);

public sealed record BrandListItem(string BrandId, string? Name, bool IsActive);
