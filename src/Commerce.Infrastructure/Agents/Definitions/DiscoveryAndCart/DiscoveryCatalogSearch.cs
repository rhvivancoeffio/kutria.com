using System.Globalization;
using System.Text.Json;
using Commerce.Application.Abstracts;
using Microsoft.Extensions.Logging;

namespace Commerce.Infrastructure.Agents.Definitions.DiscoveryAndCart;

/// <summary>
/// search_products. The model does not choose list vs bundle. Price and stock come from the catalog service.
/// When in-process catalog memory is empty (common in API after Worker indexed vectors), falls back to
/// fields already stored on the vector hit.
/// </summary>
internal static class DiscoveryCatalogSearch
{
    public const string StoreCurrency = "PEN";
    public const int DefaultLimit = 8;

    public static async Task<string> SearchAsync(
        ICatalogBrainIndex index,
        ICatalogDataService catalog,
        string tenantId,
        string? query,
        string? category,
        decimal? priceMax,
        decimal? budget,
        string? currency,
        IReadOnlyList<string>? items,
        string? occasion,
        string? referenceProductId,
        int? limit,
        CancellationToken cancellationToken,
        ILogger? logger = null,
        IReadOnlyList<CatalogOptionFilter>? optionFilters = null)
    {
        var currencyCode = string.IsNullOrWhiteSpace(currency) ? StoreCurrency : currency.Trim().ToUpperInvariant();
        var take = limit is null or < 1 ? DefaultLimit : Math.Min(limit.Value, 25);
        var slots = ResolveSlots(query, items, occasion);
        var bundleMode = budget is not null
            || Clean(items).Count > 0
            || !string.IsNullOrWhiteSpace(occasion)
            || !string.IsNullOrWhiteSpace(referenceProductId);

        if (slots.Count == 0
            && string.IsNullOrWhiteSpace(category)
            && string.IsNullOrWhiteSpace(referenceProductId)
            && (optionFilters is null || optionFilters.Count == 0))
            return Serialize(new { mode = "list", currency = currencyCode, results = Array.Empty<object>(), bundle = (object?)null, error = "missing_filter" });

        CatalogSkuSnapshot? reference = null;
        if (!string.IsNullOrWhiteSpace(referenceProductId))
        {
            reference = await FindSkuAsync(catalog, tenantId, referenceProductId.Trim(), cancellationToken);
            if (reference is null)
                return Serialize(new { mode = "bundle", currency = currencyCode, results = Array.Empty<object>(), bundle = (object?)null, error = "not_found" });
        }

        if (!bundleMode)
        {
            var ranked = await SearchSlotAsync(
                index, catalog, tenantId,
                string.IsNullOrWhiteSpace(query) ? category : query,
                category,
                priceMax,
                null,
                take,
                cancellationToken,
                logger,
                optionFilters);
            var products = GroupByProduct(ranked, take).Select(ToProductHit).ToList();
            return Serialize(new { mode = "list", currency = currencyCode, results = products, bundle = (object?)null, error = (string?)null });
        }

        if (slots.Count == 0)
            slots = [string.IsNullOrWhiteSpace(query) ? "" : query.Trim()];

        var spendBudget = budget ?? 0m;
        if (reference?.Price is { } referencePrice && budget is not null)
            spendBudget = Math.Max(0, budget.Value - referencePrice);

        var picks = new List<object>();
        var missing = new List<string>();
        var spent = reference?.Price ?? 0m;
        decimal? remaining = budget is null ? null : spendBudget;
        var ignoreCategory = Clean(items).Count > 0;
        foreach (var slot in slots)
        {
            var left = Math.Max(1, slots.Count - picks.Count);
            var cap = Cap(priceMax, remaining, left);
            var candidates = await SearchSlotAsync(
                index, catalog, tenantId, slot, ignoreCategory ? null : category, cap, reference?.Sku, 8, cancellationToken, logger, optionFilters);
            var pick = GroupByProduct(candidates, 1).FirstOrDefault()?.Primary ?? candidates.FirstOrDefault();
            if (pick is null && remaining is not null)
            {
                candidates = await SearchSlotAsync(
                    index, catalog, tenantId, slot, ignoreCategory ? null : category, priceMax, reference?.Sku, 8, cancellationToken, logger, optionFilters);
                pick = GroupByProduct(
                        candidates.Where(hit => spent + hit.Price <= (budget ?? decimal.MaxValue)).ToList(),
                        1)
                    .FirstOrDefault()?.Primary
                    ?? candidates.FirstOrDefault(hit => spent + hit.Price <= (budget ?? decimal.MaxValue));
            }

            var label = string.IsNullOrWhiteSpace(slot) ? "complemento" : slot;
            if (pick is null)
            {
                missing.Add(label);
                continue;
            }

            picks.Add(new
            {
                slot = label,
                product_id = pick.Hit.ProductId ?? pick.Sku,
                sku_id = pick.Hit.SkuId ?? pick.Sku,
                seller_id = pick.Hit.SellerId,
                name = pick.Name,
                price = pick.Price
            });
            spent += pick.Price;
            if (remaining is not null)
                remaining = Math.Max(0, remaining.Value - pick.Price);
        }

        if (reference is not null)
        {
            picks.Insert(0, new
            {
                slot = "referencia",
                product_id = reference.Sku,
                name = reference.Sku,
                price = reference.Price
            });
        }

        var bundleBudget = budget ?? spent;
        return Serialize(new
        {
            mode = "bundle",
            currency = currencyCode,
            results = picks,
            bundle = new
            {
                budget = bundleBudget,
                spent,
                remaining = Math.Max(0, bundleBudget - spent),
                items = picks,
                missing_slots = missing
            },
            error = (string?)null
        });
    }

    public static async Task<string> CompareAsync(
        ICatalogBrainIndex index,
        ICatalogDataService catalog,
        string tenantId,
        IReadOnlyList<string>? productNames,
        IReadOnlyList<string>? productIds,
        CancellationToken cancellationToken)
    {
        var names = Clean(productNames);
        var ids = Clean(productIds);
        if (names.Count is < 2 or > 4 && ids.Count is < 2 or > 4)
            return Serialize(new { products = Array.Empty<object>(), differences = Array.Empty<string>(), winner = (string?)null, error = "need_2_to_4_products" });

        var compared = new List<object>();
        if (ids.Count >= 2)
        {
            foreach (var id in ids.Take(4))
            {
                var row = await FindSkuAsync(catalog, tenantId, id, cancellationToken);
                compared.Add(row is null
                    ? new { product_id = id, name = id, price = (decimal?)null, category = (string?)null, found = false, did_you_mean = (string?)null }
                    : new { product_id = row.Sku, name = row.Sku, price = (decimal?)row.Price, category = (string?)null, found = true, did_you_mean = (string?)null });
            }
        }
        else
        {
            foreach (var name in names.Take(4))
                compared.Add(await ResolveNameAsync(index, catalog, tenantId, name, cancellationToken));
        }

        return Serialize(new
        {
            products = compared,
            differences = PriceDifferences(compared),
            winner = (string?)null,
            error = (string?)null
        });
    }

    private static async Task<object> ResolveNameAsync(
        ICatalogBrainIndex index,
        ICatalogDataService catalog,
        string tenantId,
        string name,
        CancellationToken cancellationToken)
    {
        var hits = await index.SearchAsync(tenantId, name, 5, cancellationToken);
        var snapshots = hits.Count == 0
            ? []
            : await catalog.GetBySkusAsync(tenantId, hits.Select(hit => hit.Sku).Distinct(StringComparer.OrdinalIgnoreCase).ToArray(), cancellationToken);
        var bySku = snapshots.ToDictionary(item => item.Sku, StringComparer.OrdinalIgnoreCase);
        CatalogBrainHit? suggestion = null;
        foreach (var hit in hits.OrderByDescending(hit => hit.Score))
        {
            suggestion ??= hit;
            if (!bySku.TryGetValue(hit.Sku, out var snapshot) || !CanSell(snapshot))
                continue;
            if (!Mentions(hit, name))
                continue;
            return new
            {
                product_id = snapshot.Sku,
                name,
                price = (decimal?)snapshot.Price,
                category = (string?)null,
                found = true,
                did_you_mean = (string?)null
            };
        }

        return new
        {
            product_id = (string?)null,
            name,
            price = (decimal?)null,
            category = (string?)null,
            found = false,
            did_you_mean = suggestion?.Meaning
        };
    }

    private static async Task<IReadOnlyList<Ranked>> SearchSlotAsync(
        ICatalogBrainIndex index,
        ICatalogDataService catalog,
        string tenantId,
        string? text,
        string? category,
        decimal? priceMax,
        string? excludeSku,
        int take,
        CancellationToken cancellationToken,
        ILogger? logger = null,
        IReadOnlyList<CatalogOptionFilter>? optionFilters = null)
    {
        var query = string.IsNullOrWhiteSpace(text) ? category : text;
        if (string.IsNullOrWhiteSpace(query))
            query = category;
        if (string.IsNullOrWhiteSpace(query) && optionFilters is { Count: > 0 })
            query = "productos";
        var rawTake = Math.Clamp(Math.Max(take * 8, 40), 1, 50);
        var hits = string.IsNullOrWhiteSpace(query)
            ? []
            : await index.SearchAsync(tenantId, query, rawTake, cancellationToken, optionFilters);
        if (hits.Count == 0)
            return [];

        var snapshots = await catalog.GetBySkusAsync(
            tenantId,
            hits.Select(hit => hit.Sku).Distinct(StringComparer.OrdinalIgnoreCase).ToArray(),
            cancellationToken);
        var bySku = snapshots.ToDictionary(item => item.Sku, StringComparer.OrdinalIgnoreCase);
        var missingInMemory = hits.Count(hit => !bySku.ContainsKey(hit.Sku));

        var ranked = hits
            .OrderByDescending(hit => hit.Score)
            .Select(hit =>
            {
                if (bySku.TryGetValue(hit.Sku, out var snapshot))
                    return new Ranked(hit, snapshot);
                var synthesized = TrySynthesizeSnapshot(hit);
                return synthesized is null ? null : new Ranked(hit, synthesized);
            })
            .Where(pair => pair is not null && CanSell(pair.Snapshot))
            .Select(pair => pair!)
            .Where(pair => excludeSku is null || !string.Equals(pair.Sku, excludeSku, StringComparison.OrdinalIgnoreCase))
            .Where(pair => priceMax is null || pair.Price <= priceMax)
            .Where(pair => category is null || pair.Hit.Meaning.Contains(category, StringComparison.OrdinalIgnoreCase))
            .ToList();

        var groupedCount = GroupByProduct(ranked, take).Count;
        logger?.LogInformation(
            "Catalog search enrich. TenantId={TenantId} Query={Query} VectorHits={VectorHits} MemoryHits={MemoryHits} MemoryMisses={MemoryMisses} SellableSkus={Sellable} ProductGroups={ProductGroups} SampleSkus={SampleSkus}",
            tenantId,
            query,
            hits.Count,
            bySku.Count,
            missingInMemory,
            ranked.Count,
            groupedCount,
            string.Join(',', hits.Take(5).Select(h => h.Sku)));

        return ranked;
    }

    /// <summary>
    /// API process often has empty <see cref="IIngestedCatalogMemory"/> while vectors live in Azure Search/Qdrant.
    /// Use indexed payload so discovery still returns products.
    /// </summary>
    private static CatalogSkuSnapshot? TrySynthesizeSnapshot(CatalogBrainHit hit)
    {
        if (string.IsNullOrWhiteSpace(hit.Sku))
            return null;
        if (hit.IsActive == false)
            return new CatalogSkuSnapshot(hit.Sku, hit.Stock ?? 0, hit.Price ?? 0m, Cost: 0m, Status: "paused");

        // Unknown stock from vector index: keep a positive placeholder so UI does not show "No disponible".
        var stock = hit.Stock ?? 1;
        return new CatalogSkuSnapshot(
            hit.Sku,
            stock,
            hit.Price ?? 0m,
            Cost: 0m,
            Status: "active");
    }

    private static async Task<CatalogSkuSnapshot?> FindSkuAsync(
        ICatalogDataService catalog,
        string tenantId,
        string sku,
        CancellationToken cancellationToken)
    {
        var rows = await catalog.GetBySkusAsync(tenantId, [sku], cancellationToken);
        return rows.FirstOrDefault(item => string.Equals(item.Sku, sku, StringComparison.OrdinalIgnoreCase));
    }

    private static List<string> ResolveSlots(string? query, IReadOnlyList<string>? items, string? occasion)
    {
        var slots = Clean(items);
        if (slots.Count > 0)
            return slots;
        if (string.IsNullOrWhiteSpace(occasion))
            return string.IsNullOrWhiteSpace(query) ? [] : [query.Trim()];
        return occasion.Trim().ToLowerInvariant() switch
        {
            "outfit" or "casual" => ["top", "bottom"],
            "matrimonio" => ["camisa formal", "pantalon formal"],
            "regalo" => [string.IsNullOrWhiteSpace(query) ? "regalo" : query.Trim()],
            _ => [occasion.Trim()]
        };
    }

    private static decimal? Cap(decimal? priceMax, decimal? remaining, int slotsLeft)
    {
        decimal? share = remaining is null || slotsLeft < 1 ? null : remaining.Value / slotsLeft;
        if (priceMax is null)
            return share;
        if (share is null)
            return priceMax;
        return Math.Min(priceMax.Value, share.Value);
    }

    // Temporary (testing): do not hide by stock or paused status — UI shows "No disponible".
    private static bool CanSell(CatalogSkuSnapshot snapshot) => true;

    private static bool Mentions(CatalogBrainHit hit, string name)
    {
        var tokens = name.Split([' ', '-', '/'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Where(token => token.Length >= 3);
        var text = hit.Meaning + " " + hit.Sku;
        return tokens.Any(token => text.Contains(token, StringComparison.OrdinalIgnoreCase));
    }

    private static IReadOnlyList<ProductGroup> GroupByProduct(IReadOnlyList<Ranked> ranked, int take)
    {
        if (ranked.Count == 0 || take < 1)
            return [];

        return ranked
            .GroupBy(
                r => string.IsNullOrWhiteSpace(r.Hit.ProductId) ? r.Sku : r.Hit.ProductId!,
                StringComparer.OrdinalIgnoreCase)
            .OrderByDescending(g => g.Max(x => x.Hit.Score))
            .Take(take)
            .Select(g =>
            {
                var ordered = g.OrderByDescending(x => x.Hit.Score).ToList();
                return new ProductGroup(g.Key, ordered[0], ordered);
            })
            .ToList();
    }

    private static object ToProductHit(ProductGroup group)
    {
        var primary = group.Primary;
        var sizes = DistinctOptionValues(group.Variants, "SIZE");
        var colors = DistinctOptionValues(group.Variants, "COLOR");
        var size = OptionValue(primary.Hit.OptionPairs, "SIZE");
        var color = OptionValue(primary.Hit.OptionPairs, "COLOR");
        var priceFrom = group.Variants.Min(v => v.Price);
        var priceTo = group.Variants.Max(v => v.Price);
        var summary = BuildOptionsSummary(sizes, colors);
        var why = !string.IsNullOrWhiteSpace(summary)
            ? summary
            : Trim(primary.Hit.Meaning);

        return new
        {
            product_id = group.Key,
            sku = primary.Sku,
            sku_id = primary.Hit.SkuId ?? primary.Sku,
            seller_id = primary.Hit.SellerId,
            name = primary.Name,
            price = primary.Price,
            price_from = priceFrom,
            price_to = priceTo > priceFrom ? priceTo : (decimal?)null,
            category = (string?)null,
            image_url = primary.Hit.ImageUrl,
            stock = primary.Snapshot.Stock,
            why,
            size,
            color,
            sizes,
            colors,
            options_summary = summary,
            variant_count = group.Variants.Count,
            variants = group.Variants.Take(16).Select(v => new
            {
                sku_id = v.Hit.SkuId ?? v.Sku,
                seller_id = v.Hit.SellerId,
                size = OptionValue(v.Hit.OptionPairs, "SIZE"),
                color = OptionValue(v.Hit.OptionPairs, "COLOR"),
                stock = v.Snapshot.Stock,
                price = v.Price,
                image_url = v.Hit.ImageUrl
            }).ToList()
        };
    }

    private static List<string> DistinctOptionValues(IEnumerable<Ranked> variants, string key)
    {
        var values = new List<string>();
        foreach (var variant in variants)
        {
            var value = OptionValue(variant.Hit.OptionPairs, key);
            if (string.IsNullOrWhiteSpace(value))
                continue;
            if (!values.Contains(value, StringComparer.OrdinalIgnoreCase))
                values.Add(value);
        }

        return values;
    }

    private static string? OptionValue(IReadOnlyList<string>? pairs, string key)
    {
        if (pairs is null || pairs.Count == 0)
            return null;
        var prefix = key + ":";
        foreach (var pair in pairs)
        {
            if (pair.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                return pair[prefix.Length..].Trim();
        }

        return null;
    }

    private static string? BuildOptionsSummary(IReadOnlyList<string> sizes, IReadOnlyList<string> colors)
    {
        var parts = new List<string>();
        if (sizes.Count == 1)
            parts.Add($"Talla {sizes[0]}");
        else if (sizes.Count > 1)
            parts.Add($"Tallas {string.Join(", ", sizes.Take(6))}{(sizes.Count > 6 ? "…" : "")}");

        if (colors.Count == 1)
            parts.Add(colors[0]);
        else if (colors.Count > 1)
            parts.Add($"{colors.Count} colores");

        return parts.Count == 0 ? null : string.Join(" · ", parts);
    }

    private static List<string> PriceDifferences(IReadOnlyList<object> products)
    {
        var prices = new List<string>();
        foreach (var product in products)
        {
            var json = JsonSerializer.Serialize(product);
            using var doc = JsonDocument.Parse(json);
            if (doc.RootElement.TryGetProperty("found", out var found) && found.ValueKind == JsonValueKind.False)
                continue;
            if (doc.RootElement.TryGetProperty("price", out var price) && price.ValueKind == JsonValueKind.Number)
                prices.Add(price.GetDecimal().ToString(CultureInfo.InvariantCulture));
        }

        return prices.Count < 2 ? [] : ["precio: " + string.Join(" vs ", prices)];
    }

    private static List<string> Clean(IReadOnlyList<string>? values) =>
        values?.Where(value => !string.IsNullOrWhiteSpace(value)).Select(value => value.Trim()).Distinct(StringComparer.OrdinalIgnoreCase).ToList()
        ?? [];

    private static string Trim(string meaning) =>
        meaning.Length <= 140 ? meaning : meaning[..140];

    private static string Serialize(object value) => JsonSerializer.Serialize(value);

    private sealed record Ranked(CatalogBrainHit Hit, CatalogSkuSnapshot Snapshot)
    {
        public string Sku => Snapshot.Sku;
        public string Name =>
            !string.IsNullOrWhiteSpace(Hit.Title) ? Hit.Title!
            : !string.IsNullOrWhiteSpace(Hit.Meaning) ? Hit.Meaning
            : Snapshot.Sku;
        public decimal Price => Snapshot.Price > 0 ? Snapshot.Price : Hit.Price ?? 0m;
    }

    private sealed record ProductGroup(string Key, Ranked Primary, IReadOnlyList<Ranked> Variants);
}
