namespace Commerce.Application.Abstracts;

public sealed class CatalogImageOptions
{
    public const string SectionName = "CatalogImage";

    /// <summary>Max parallel image-queue consumers.</summary>
    public int MaxDegree { get; set; } = 2;

    /// <summary>
    /// Max image angles to index per product (canonical gallery), not per SKU/size.
    /// </summary>
    public int MaxImagesPerProduct { get; set; } = 7;
}
