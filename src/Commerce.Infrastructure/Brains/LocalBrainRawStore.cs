using Microsoft.Extensions.Configuration;
using Commerce.Application.Abstracts;

namespace Commerce.Infrastructure.Brains;

public sealed class LocalBrainRawStore : IBrainRawStore
{
    private readonly string _root;

    public LocalBrainRawStore(IConfiguration configuration)
    {
        var configured = configuration["Brain:RawPath"];
        _root = string.IsNullOrWhiteSpace(configured)
            ? Path.Combine(Path.GetTempPath(), "commerce-brains-raw")
            : configured;
    }

    public async Task<string> SaveAsync(
        string tenantId,
        Guid workspaceId,
        string brainKey,
        Guid jobId,
        string fileName,
        ReadOnlyMemory<byte> content,
        CancellationToken cancellationToken = default)
    {
        var safeName = Sanitize(fileName);
        var relative = Path.Combine(
            Sanitize(tenantId),
            workspaceId.ToString("N"),
            Sanitize(brainKey),
            jobId.ToString("N"),
            safeName);
        var full = Path.Combine(_root, relative);
        Directory.CreateDirectory(Path.GetDirectoryName(full)!);
        await File.WriteAllBytesAsync(full, content.ToArray(), cancellationToken);
        return relative.Replace('\\', '/');
    }

    public async Task<byte[]> ReadAsync(string storageKey, CancellationToken cancellationToken = default)
    {
        var full = FullPath(storageKey);
        return await File.ReadAllBytesAsync(full, cancellationToken);
    }

    public string ToSourceUrl(string storageKey)
        => "brains-raw/" + storageKey.Replace('\\', '/');

    private string FullPath(string storageKey)
    {
        var relative = storageKey.Replace('/', Path.DirectorySeparatorChar);
        var full = Path.GetFullPath(Path.Combine(_root, relative));
        var root = Path.GetFullPath(_root);
        if (!full.StartsWith(root, StringComparison.Ordinal))
        {
            throw new InvalidOperationException("Storage key is outside the raw store.");
        }

        return full;
    }

    private static string Sanitize(string value)
    {
        var name = Path.GetFileName(value);
        var invalid = Path.GetInvalidFileNameChars();
        var cleaned = new string(name.Select(ch => invalid.Contains(ch) ? '_' : ch).ToArray());
        return string.IsNullOrWhiteSpace(cleaned) ? "file" : cleaned;
    }
}
