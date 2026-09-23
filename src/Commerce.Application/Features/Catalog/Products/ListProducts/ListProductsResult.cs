namespace Commerce.Application.Features.Catalog.Products.ListProducts;

public sealed record ListProductsResult(
    int CurrentPage,
    int PageCount,
    int PageSize,
    int RowCount,
    IReadOnlyList<ProductListItem> Items);

public sealed record ProductListItem(
    string ProductId,
    string? Name,
    string? ProductStatusName,
    DateTimeOffset? UpdatedOn,
    string? ImageUrl,
    string? BrandName,
    string? CategoryPath,
    decimal? Stock,
    decimal? BasePrice,
    decimal? SpecialPrice,
    string? CurrencySymbol);
