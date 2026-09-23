namespace Commerce.Application.Abstracts;

public sealed record CatalogImageVectorResult(string? Caption, IReadOnlyList<float>? Vector)
{
    public static CatalogImageVectorResult Empty { get; } = new(null, null);

    public bool HasVector => Vector is { Count: > 0 };
}

/// <summary>
/// Verbalize + embed product images once per unique URL (tenant-scoped), shared by Azure Search and Qdrant upserts.
/// </summary>
public interface ICatalogImageVectorService
{
    Task<CatalogImageVectorResult> ResolveAsync(
        string tenantId,
        string? imageUrl,
        CancellationToken cancellationToken = default);
}

public static class CatalogImageUrl
{
    public static string? Normalize(string? imageUrl)
    {
        if (string.IsNullOrWhiteSpace(imageUrl))
            return null;

        var raw = imageUrl.Trim();
        if (!Uri.TryCreate(raw, UriKind.Absolute, out var uri)
            || (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps))
        {
            return raw;
        }

        var builder = new UriBuilder(uri)
        {
            Host = uri.Host.ToLowerInvariant(),
            Fragment = string.Empty
        };
        return builder.Uri.AbsoluteUri;
    }
}
