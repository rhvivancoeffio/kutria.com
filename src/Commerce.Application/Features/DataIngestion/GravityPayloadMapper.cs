using System.Text.Json;

namespace Commerce.Application.Features.DataIngestion;

internal static class GravityPayloadMapper
{
    public static CatalogSummary MapCatalog(string? id, string? name, string? status, string? payloadJson)
    {
        if (string.IsNullOrWhiteSpace(payloadJson))
        {
            return new CatalogSummary(id ?? string.Empty, name, status, null, null, null, null, null, null, null, null, null);
        }

        try
        {
            using var doc = JsonDocument.Parse(payloadJson);
            var item = doc.RootElement;
            var (basePrice, specialPrice) = ReadPrices(item);
            return new CatalogSummary(
                ReadString(item, "productId") ?? id ?? string.Empty,
                ReadString(item, "name") ?? name,
                ReadString(item, "productStatusName") ?? status,
                ReadFirstImageUrl(item),
                ReadString(item, "sellerName") ?? ReadString(item, "principalSellerName"),
                ReadString(item, "brandName"),
                ReadString(item, "categoryPath"),
                ReadString(item, "productExternalId"),
                ReadDecimal(item, "totalStock"),
                basePrice,
                specialPrice,
                ReadString(item, "currencySymbol") ?? ReadString(item, "currency"));
        }
        catch (JsonException)
        {
            return new CatalogSummary(id ?? string.Empty, name, status, null, null, null, null, null, null, null, null, null);
        }
    }

    public static OrderSummary MapOrder(string? id, string? name, string? status, string? payloadJson)
    {
        if (string.IsNullOrWhiteSpace(payloadJson))
        {
            return new OrderSummary(
                id ?? string.Empty,
                name,
                status,
                name,
                null,
                null,
                null,
                null,
                null,
                null,
                null,
                null,
                null);
        }

        try
        {
            using var doc = JsonDocument.Parse(payloadJson);
            var item = doc.RootElement;
            var orderNumber = ReadString(item, "orderReferenceNumber")
                ?? ReadString(item, "saleOrderGroup")
                ?? name
                ?? id;

            var mappedStatus = ReadString(item, "saleOrderCustomStatusName")
                ?? ReadString(item, "saleOrderStatusName")
                ?? ReadString(item, "status")
                ?? status;

            string? clientName = null;
            string? clientSecondary = null;
            if (item.TryGetProperty("client", out var client) && client.ValueKind == JsonValueKind.Object)
            {
                clientName = ReadString(client, "fullName")
                    ?? JoinNames(ReadString(client, "firstName"), ReadString(client, "lastName"))
                    ?? ReadString(client, "entityName");
                clientSecondary = ReadString(client, "phone")
                    ?? ReadString(client, "identificationNumber")
                    ?? ReadString(client, "entityIdentificationNumber");
            }

            string? sellerName = null;
            if (item.TryGetProperty("seller", out var seller) && seller.ValueKind == JsonValueKind.Object)
                sellerName = ReadString(seller, "name");

            var providers = new List<string>();
            if (item.TryGetProperty("saleOrderProviderTenants", out var tenants) && tenants.ValueKind == JsonValueKind.Array)
            {
                foreach (var tenant in tenants.EnumerateArray())
                {
                    var providerName = ReadString(tenant, "providerName");
                    if (!string.IsNullOrWhiteSpace(providerName)
                        && !providers.Contains(providerName, StringComparer.OrdinalIgnoreCase))
                        providers.Add(providerName);
                }
            }

            return new OrderSummary(
                ReadString(item, "saleOrderId") ?? id ?? string.Empty,
                orderNumber,
                mappedStatus,
                orderNumber,
                ReadDate(item, "orderDate"),
                ReadDate(item, "shippingDate")
                    ?? ReadDate(item, "shippingOrderDate")
                    ?? ReadDate(item, "completedOrderDate"),
                clientName,
                clientSecondary,
                sellerName,
                ReadOrderItemCount(item),
                ReadDecimal(item, "total"),
                ReadString(item, "currencySymbol") ?? ReadString(item, "currencyIsoCode"),
                providers.Count > 0 ? providers : null);
        }
        catch (JsonException)
        {
            return new OrderSummary(
                id ?? string.Empty,
                name,
                status,
                name,
                null,
                null,
                null,
                null,
                null,
                null,
                null,
                null,
                null);
        }
    }

    internal sealed record CatalogSummary(
        string Id,
        string? Name,
        string? Status,
        string? ImageUrl,
        string? SellerName,
        string? BrandName,
        string? CategoryPath,
        string? MarketplaceId,
        decimal? Stock,
        decimal? BasePrice,
        decimal? SpecialPrice,
        string? CurrencySymbol);

    internal sealed record OrderSummary(
        string Id,
        string? Name,
        string? Status,
        string? OrderNumber,
        DateTimeOffset? OrderDate,
        DateTimeOffset? DeliveryDate,
        string? ClientName,
        string? ClientSecondary,
        string? SellerName,
        int? ItemCount,
        decimal? Total,
        string? CurrencySymbol,
        IReadOnlyList<string>? ProviderNames);

    private static (decimal? BasePrice, decimal? SpecialPrice) ReadPrices(JsonElement item)
    {
        decimal? basePrice = null;
        decimal? specialPrice = null;

        if (item.TryGetProperty("prices", out var prices) && prices.ValueKind == JsonValueKind.Object)
        {
            basePrice = ReadDecimal(prices, "maxBasePrice") ?? ReadDecimal(prices, "minBasePrice");
            specialPrice = ReadDecimal(prices, "minSpecialPrice") ?? ReadDecimal(prices, "maxSpecialPrice");
        }

        basePrice ??= ReadDecimal(item, "totalPrice");
        var totalSpecial = ReadDecimal(item, "totalSpecialPrice");
        if (totalSpecial is > 0)
            specialPrice ??= totalSpecial;

        if (item.TryGetProperty("skus", out var skus) && skus.ValueKind == JsonValueKind.Array)
        {
            foreach (var sku in skus.EnumerateArray())
            {
                basePrice ??= ReadDecimal(sku, "basePrice");
                var skuSpecial = ReadDecimal(sku, "specialPrice");
                if (skuSpecial is > 0)
                    specialPrice ??= skuSpecial;
                if (basePrice is not null && specialPrice is not null)
                    break;
            }
        }

        if (specialPrice is 0)
            specialPrice = null;

        return (basePrice, specialPrice);
    }

    private static string? ReadFirstImageUrl(JsonElement item)
    {
        if (!item.TryGetProperty("skus", out var skus) || skus.ValueKind != JsonValueKind.Array)
            return null;

        foreach (var sku in skus.EnumerateArray())
        {
            if (!sku.TryGetProperty("imageList", out var images) || images.ValueKind != JsonValueKind.Array)
                continue;
            foreach (var image in images.EnumerateArray())
            {
                var link = ReadString(image, "link");
                if (!string.IsNullOrWhiteSpace(link))
                    return link;
            }
        }

        return null;
    }

    private static int? ReadOrderItemCount(JsonElement item)
    {
        if (!item.TryGetProperty("saleOrderItems", out var items) || items.ValueKind != JsonValueKind.Array)
            return null;

        decimal totalQty = 0;
        var hasQty = false;
        var count = 0;
        foreach (var line in items.EnumerateArray())
        {
            count++;
            var qty = ReadDecimal(line, "qty");
            if (qty is null) continue;
            hasQty = true;
            totalQty += qty.Value;
        }

        if (count == 0) return 0;
        if (hasQty) return (int)Math.Round(totalQty, MidpointRounding.AwayFromZero);
        return count;
    }

    private static string? JoinNames(string? first, string? last)
    {
        var value = $"{first} {last}".Trim();
        return string.IsNullOrWhiteSpace(value) ? null : value;
    }

    private static string? ReadString(JsonElement el, string name)
        => el.TryGetProperty(name, out var p) && p.ValueKind == JsonValueKind.String ? p.GetString() : null;

    private static decimal? ReadDecimal(JsonElement el, string name)
    {
        if (!el.TryGetProperty(name, out var p))
            return null;
        if (p.ValueKind == JsonValueKind.Number && p.TryGetDecimal(out var d))
            return d;
        if (p.ValueKind == JsonValueKind.String && decimal.TryParse(p.GetString(), out var parsed))
            return parsed;
        return null;
    }

    private static DateTimeOffset? ReadDate(JsonElement el, string name)
    {
        if (!el.TryGetProperty(name, out var p))
            return null;
        if (p.ValueKind == JsonValueKind.String && DateTimeOffset.TryParse(p.GetString(), out var dto))
            return dto;
        return null;
    }
}
