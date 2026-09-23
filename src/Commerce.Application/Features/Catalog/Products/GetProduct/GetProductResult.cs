namespace Commerce.Application.Features.Catalog.Products.GetProduct;

public sealed record GetProductResult(
    string ProductId,
    string? Name,
    string? ProductStatusName,
    DateTimeOffset? UpdatedOn,
    string PayloadJson);
