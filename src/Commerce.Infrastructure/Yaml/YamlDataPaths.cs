namespace Commerce.Infrastructure.Yaml;

/// <summary>
/// Prefers <c>data/</c> copied next to the app (Docker/Aspire), then walks up from the
/// current directory for a local checkout.
/// </summary>
internal static class YamlDataPaths
{
    public static string ResolveDataRootDirectory()
    {
        var besideApp = Path.Combine(AppContext.BaseDirectory, "data");
        if (Directory.Exists(besideApp))
        {
            return besideApp;
        }

        var dir = new DirectoryInfo(Directory.GetCurrentDirectory());
        while (dir is not null)
        {
            var candidate = Path.Combine(dir.FullName, "data");
            if (Directory.Exists(candidate))
            {
                return candidate;
            }

            dir = dir.Parent;
        }

        return besideApp;
    }

    public static string? TryResolveExistingFilePath(string relativePathUnderData)
    {
        var path = Path.GetFullPath(Path.Combine(ResolveDataRootDirectory(), Normalize(relativePathUnderData)));
        return File.Exists(path) ? path : null;
    }

    public static string Normalize(string relativePathUnderData)
    {
        var trimmed = relativePathUnderData.Trim().Replace('/', Path.DirectorySeparatorChar);
        return trimmed.TrimStart(Path.DirectorySeparatorChar);
    }
}
