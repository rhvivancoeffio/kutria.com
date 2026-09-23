using Commerce.Application.Abstracts;
using Commerce.Application.Common.Abstracts;

namespace Commerce.Application.Features.Catalog.Products.CreateProduct;

public sealed record CreateProductCommand(
    string Name,
    string Description,
    string? BrandId = null,
    string? BrandName = null,
    string? CategoryId = null,
    string? CategoryPath = null,
    string? ImageUrl = null,
    bool IsActive = true,
    bool ShowInCatalog = true,
    Guid? IntegrationId = null,
    IReadOnlyList<CreateProductVariationInput>? Variations = null) : ICommand<CreateProductResult>;

public sealed record CreateProductVariationInput(
    string Name,
    string? Sku = null,
    string? VariantName = null,
    decimal BasePrice = 0,
    int Stock = 0,
    string? ImageUrl = null,
    IReadOnlyList<CreateProductVariationOptionInput>? Options = null);

public sealed record CreateProductVariationOptionInput(
    string Key,
    string Name,
    string? Value = null);
