namespace Commerce.Application.Abstracts;

public static class CatalogDocTypes
{
    public const string Sku = "sku";
    public const string Image = "image";
}

/// <summary>Image-angle document stored alongside SKU catalog docs.</summary>
public sealed record CatalogImageDocument(
    string TenantId,
    string ProductId,
    string Sku,
    string ImageUrl,
    IReadOnlyList<float> ImageVector,
    string? Title = null,
    string? Brand = null,
    string? Seller = null,
    decimal Price = 0,
    bool IsActive = true,
    int Stock = 0,
    string? SkuId = null,
    string? SellerId = null);
