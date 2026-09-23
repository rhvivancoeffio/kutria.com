namespace Commerce.Application.Abstracts;

public sealed record CatalogOptionFilter(string Key, string Value);

public static class CatalogOptionNormalizer
{
    private static readonly HashSet<string> Allowlist = new(StringComparer.OrdinalIgnoreCase)
    {
        "GENDER",
        "AGE_GROUP",
        "COLOR",
        "SIZE",
        "ITEM_CONDITION"
    };

    private static readonly Dictionary<string, string> KeyAliases = new(StringComparer.OrdinalIgnoreCase)
    {
        ["GENERO"] = "GENDER",
        ["GÉNERO"] = "GENDER",
        ["SEXO"] = "GENDER",
        ["TALLA"] = "SIZE",
        ["TALLES"] = "SIZE",
        ["COLOUR"] = "COLOR",
        ["CONDICION"] = "ITEM_CONDITION",
        ["CONDICIÓN"] = "ITEM_CONDITION",
        ["EDAD"] = "AGE_GROUP"
    };

    private static readonly Dictionary<string, Dictionary<string, string>> ValueAliases =
        new(StringComparer.OrdinalIgnoreCase)
        {
            ["GENDER"] = new(StringComparer.OrdinalIgnoreCase)
            {
                ["hombre"] = "Hombre",
                ["men"] = "Hombre",
                ["male"] = "Hombre",
                ["masculino"] = "Hombre",
                ["mujer"] = "Mujer",
                ["women"] = "Mujer",
                ["woman"] = "Mujer",
                ["female"] = "Mujer",
                ["femenino"] = "Mujer",
                ["unisex"] = "Unisex"
            },
            ["AGE_GROUP"] = new(StringComparer.OrdinalIgnoreCase)
            {
                ["adultos"] = "Adultos",
                ["adult"] = "Adultos",
                ["kids"] = "Niños",
                ["ninos"] = "Niños",
                ["niños"] = "Niños"
            }
        };

    public static string FormatPair(string canonicalKey, string canonicalValue)
        => $"{canonicalKey}:{canonicalValue}";

    public static bool TryNormalize(string? rawKey, string? rawValue, out string canonicalKey, out string pair)
    {
        canonicalKey = string.Empty;
        pair = string.Empty;
        if (!TryNormalize(rawKey, rawValue, out canonicalKey, out var canonicalValue, out pair))
            return false;
        _ = canonicalValue;
        return true;
    }

    public static bool TryNormalize(
        string? rawKey,
        string? rawValue,
        out string canonicalKey,
        out string canonicalValue,
        out string pair)
    {
        canonicalKey = string.Empty;
        canonicalValue = string.Empty;
        pair = string.Empty;
        if (string.IsNullOrWhiteSpace(rawKey) || string.IsNullOrWhiteSpace(rawValue))
            return false;

        var key = rawKey.Trim().ToUpperInvariant();
        if (KeyAliases.TryGetValue(key, out var aliasedKey))
            key = aliasedKey;
        else if (KeyAliases.TryGetValue(rawKey.Trim(), out aliasedKey))
            key = aliasedKey;

        if (!Allowlist.Contains(key))
            return false;

        var value = rawValue.Trim();
        if (ValueAliases.TryGetValue(key, out var map) && map.TryGetValue(value, out var aliasedValue))
            value = aliasedValue;

        canonicalKey = key;
        canonicalValue = value;
        pair = FormatPair(key, value);
        return true;
    }

    public static IReadOnlyList<CatalogOptionFilter> BuildFilters(
        string? gender = null,
        string? size = null,
        string? color = null,
        IEnumerable<string>? rawPairs = null)
    {
        var filters = new List<CatalogOptionFilter>();
        if (!string.IsNullOrWhiteSpace(gender))
            filters.Add(new CatalogOptionFilter("GENDER", gender));
        if (!string.IsNullOrWhiteSpace(size))
            filters.Add(new CatalogOptionFilter("SIZE", size));
        if (!string.IsNullOrWhiteSpace(color))
            filters.Add(new CatalogOptionFilter("COLOR", color));

        if (rawPairs is not null)
        {
            foreach (var raw in rawPairs)
            {
                if (string.IsNullOrWhiteSpace(raw))
                    continue;
                var idx = raw.IndexOf(':');
                if (idx <= 0 || idx >= raw.Length - 1)
                    continue;
                filters.Add(new CatalogOptionFilter(raw[..idx], raw[(idx + 1)..]));
            }
        }

        return filters;
    }

    public static IReadOnlyList<string> NormalizeFilters(IEnumerable<CatalogOptionFilter>? filters)
    {
        if (filters is null)
            return [];

        var pairs = new List<string>();
        foreach (var filter in filters)
        {
            if (!TryNormalize(filter.Key, filter.Value, out _, out var pair))
                continue;
            if (!pairs.Contains(pair, StringComparer.OrdinalIgnoreCase))
                pairs.Add(pair);
        }

        return pairs;
    }
}
