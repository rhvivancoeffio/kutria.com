using Commerce.Application.Common.Abstracts;

namespace Commerce.Application.Features.Catalog.Products.UpdateProduct;

public sealed record UpdateProductCommand(
    string ProductId,
    string Name,
    string Description,
    string? BrandId = null,
    string? BrandName = null,
    string? CategoryId = null,
    string? CategoryPath = null,
    string? ImageUrl = null,
    bool IsActive = true,
    bool ShowInCatalog = true,
    Guid? IntegrationId = null) : ICommand<UpdateProductResult>;
