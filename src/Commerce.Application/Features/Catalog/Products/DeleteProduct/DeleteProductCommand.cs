using Commerce.Application.Common.Abstracts;

namespace Commerce.Application.Features.Catalog.Products.DeleteProduct;

public sealed record DeleteProductCommand(string ProductId, Guid? IntegrationId = null)
    : ICommand<DeleteProductResult>;
