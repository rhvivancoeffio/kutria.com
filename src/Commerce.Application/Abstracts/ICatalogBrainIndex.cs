namespace Commerce.Application.Abstracts;

public sealed record CatalogBrainHit(
    string Sku,
    string Meaning,
    float Score,
    string? Title = null,
    decimal? Price = null,
    bool? IsActive = null,
    string? ImageUrl = null,
    string? Brand = null,
    string? Seller = null,
    int? Stock = null,
    string? ProductId = null,
    string? SkuId = null,
    string? SellerId = null,
    IReadOnlyList<string>? OptionPairs = null);

public sealed record CatalogProductDocument(
    string TenantId,
    string Sku,
    string Title,
    string Description,
    string? Category,
    decimal Price,
    bool IsActive,
    string? ImageUrl,
    string? Brand = null,
    string? Seller = null,
    string? ProductId = null,
    string? SkuId = null,
    string? SellerId = null,
    int Stock = 0,
    IReadOnlyList<string>? OptionKeys = null,
    IReadOnlyList<string>? OptionPairs = null);

public interface ICatalogBrainIndex
{
    Task<IReadOnlyList<CatalogBrainHit>> SearchAsync(
        string tenantId,
        string text,
        int limit,
        CancellationToken cancellationToken = default,
        IReadOnlyList<CatalogOptionFilter>? optionFilters = null);
}

public interface IVectorIndexBootstrapper
{
    Task EnsureAsync(CancellationToken cancellationToken = default);
}
