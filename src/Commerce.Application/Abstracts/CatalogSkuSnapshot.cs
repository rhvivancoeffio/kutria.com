namespace Commerce.Application.Abstracts;

public sealed record CatalogSkuSnapshot(string Sku, int Stock, decimal Price, decimal Cost, string Status);
