namespace Commerce.Application.Features.Catalog.Brands.AutocompleteBrands;

public sealed record AutocompleteBrandsResult(IReadOnlyList<BrandAutocompleteItem> Items);

public sealed record BrandAutocompleteItem(string BrandId, string? Name);
