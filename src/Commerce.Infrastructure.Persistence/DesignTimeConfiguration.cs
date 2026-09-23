using Microsoft.Extensions.Configuration;

namespace Commerce.Infrastructure.Persistence;

/// <summary>
/// Shared design-time config resolution (same pattern as content-builder DesignTime factories).
/// <c>dotnet ef</c> uses the migrations project as CWD; walk up to <c>src/Commerce.Api</c>.
/// </summary>
internal static class DesignTimeConfiguration
{
    public static IConfiguration Build(string[]? args = null)
    {
        var basePath = ResolveApiDirectory(Directory.GetCurrentDirectory());

        return new ConfigurationBuilder()
            .SetBasePath(basePath)
            .AddJsonFile("appsettings.json", optional: true)
            .AddJsonFile(
                $"appsettings.{Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Development"}.json",
                optional: true)
            .AddEnvironmentVariables()
            .AddCommandLine(args ?? [])
            .Build();
    }

    /// <summary>
    /// Resolves <c>ConnectionStrings__Commerce</c>. No hardcoded defaults.
    /// </summary>
    public static string ResolveConnectionString(IConfiguration configuration, string provider)
    {
        // provider selects which EF context the design-time factory builds; connection name is always Commerce
        _ = provider;
        var connectionString = configuration.GetConnectionString("Commerce");
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException(
                "Connection string 'Commerce' is required. Set ConnectionStrings__Commerce (or ConnectionStrings:Commerce in appsettings).");
        }

        return connectionString;
    }

    private static string ResolveApiDirectory(string start)
    {
        var dir = new DirectoryInfo(start);
        while (dir != null)
        {
            var candidate = Path.Combine(dir.FullName, "src", "Commerce.Api");
            if (Directory.Exists(candidate))
            {
                return candidate;
            }

            dir = dir.Parent;
        }

        return start;
    }
}
