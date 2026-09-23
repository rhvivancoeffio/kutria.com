using Commerce.Application.Common.Abstracts;

namespace Commerce.Application.Features.Catalog.Products.GetProduct;

public sealed record GetProductQuery(string ProductId, Guid? IntegrationId = null) : IQuery<GetProductResult?>;
