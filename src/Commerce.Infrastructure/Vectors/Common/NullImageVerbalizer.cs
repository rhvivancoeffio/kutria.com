namespace Commerce.Infrastructure.Vectors.Common;

public sealed class NullImageVerbalizer : IImageVerbalizer
{
    public Task<string?> VerbalizeAsync(
        Stream image,
        string? contentType = null,
        CancellationToken cancellationToken = default)
        => Task.FromResult<string?>(null);

    public Task<string?> VerbalizeUrlAsync(string imageUrl, CancellationToken cancellationToken = default)
        => Task.FromResult<string?>(null);

    public Task<ProductImageAnalysis?> AnalyzeProductAsync(
        Stream image,
        string? contentType = null,
        string? hint = null,
        CancellationToken cancellationToken = default)
        => Task.FromResult<ProductImageAnalysis?>(null);

    public Task<ProductImageAnalysis?> AnalyzeProductUrlAsync(
        string imageUrl,
        string? hint = null,
        CancellationToken cancellationToken = default)
        => Task.FromResult<ProductImageAnalysis?>(null);
}
