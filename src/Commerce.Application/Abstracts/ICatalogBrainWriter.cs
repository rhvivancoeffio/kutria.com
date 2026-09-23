namespace Commerce.Application.Abstracts;

public interface ICatalogBrainWriter
{
    Task UpsertAsync(string tenantId, string sku, string meaning, CancellationToken cancellationToken = default);

    Task UpsertAsync(CatalogProductDocument document, CancellationToken cancellationToken = default);

    /// <summary>
    /// Upsert a <c>doc_type=image</c> document keyed by tenant + normalized image URL
    /// (one vector per unique catalog photo / angle).
    /// </summary>
    Task UpsertImageDocumentAsync(
        CatalogImageDocument document,
        CancellationToken cancellationToken = default);
}
