using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace Commerce.Application.Features.Auth;

public static partial class TenantIdentifierRules
{
    public const int MinLength = 3;
    public const int MaxLength = 32;

    private static readonly HashSet<string> Reserved = new(StringComparer.OrdinalIgnoreCase)
    {
        "www", "api", "admin", "app", "auth", "mail", "static", "assets",
        "localhost", "kutria", "health", "swagger", "oauth", "signup", "signin"
    };

    public static string Normalize(string? value)
        => (value ?? string.Empty).Trim().ToLowerInvariant();

    /// <summary>
    /// Turns a shop name such as "Mi tienda favorita" into "mi-tienda-favorita".
    /// </summary>
    public static string Slugify(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        var decomposed = value.Trim().Normalize(NormalizationForm.FormD);
        var builder = new StringBuilder(decomposed.Length);
        var pendingHyphen = false;
        foreach (var ch in decomposed)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(ch) == UnicodeCategory.NonSpacingMark)
            {
                continue;
            }

            var lower = char.ToLowerInvariant(ch);
            if (lower is >= 'a' and <= 'z' or >= '0' and <= '9')
            {
                if (pendingHyphen && builder.Length > 0)
                {
                    builder.Append('-');
                }

                pendingHyphen = false;
                if (builder.Length >= MaxLength)
                {
                    break;
                }

                builder.Append(lower);
                continue;
            }

            pendingHyphen = builder.Length > 0;
        }

        return builder.ToString().TrimEnd('-');
    }

    public static bool IsReserved(string identifier) => Reserved.Contains(identifier);

    public static bool IsFormatValid(string identifier)
        => identifier.Length is >= MinLength and <= MaxLength && SlugPattern().IsMatch(identifier);

    [GeneratedRegex("^[a-z0-9]+(?:-[a-z0-9]+)*$", RegexOptions.CultureInvariant)]
    private static partial Regex SlugPattern();
}
