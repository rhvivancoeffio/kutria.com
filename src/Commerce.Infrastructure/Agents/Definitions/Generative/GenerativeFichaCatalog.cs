using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Commerce.Application.Abstracts;
using Commerce.Application.Features.Catalog;

namespace Commerce.Infrastructure.Agents.Definitions.Generative;

/// <summary>
/// Loads Gravity categories for generative prompts and normalizes ficha brand/category create-vs-reuse flags.
/// </summary>
internal static class GenerativeFichaCatalog
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);
    private const int MaxCategoriesInPrompt = 250;

    public static async Task<(string PromptBlock, IReadOnlyList<GravityCategoryItem> Categories)> LoadCategoryPromptAsync(
        IServiceProvider services,
        CancellationToken cancellationToken)
    {
        try
        {
            var db = services.GetRequiredService<ICommerceDbContext>();
            var gravity = services.GetRequiredService<IGravityStoreDataClient>();
            var logger = services.GetRequiredService<ILoggerFactory>().CreateLogger("GenerativeFichaCatalog");
            var workspaceContext = services.GetRequiredService<IWorkspaceContext>();
            var workspaceId = workspaceContext.WorkspaceId
                ?? throw new InvalidOperationException("Workspace is required.");
            var (_, credentials) = await CatalogGravityCredentials.LoadActiveAsync(
                db,
                logger,
                workspaceId,
                integrationId: null,
                cancellationToken);
            var categories = await gravity.ListCategoriesAsync(credentials, name: null, cancellationToken);
            return (BuildCategoryPromptBlock(categories), categories);
        }
        catch (Exception ex)
        {
            var logger = services.GetService<ILoggerFactory>()?.CreateLogger("GenerativeFichaCatalog");
            logger?.LogWarning(ex, "Could not load Gravity categories for generative ficha");
            return (
                "CATEGORIAS_GRAVITY: (no disponibles). Sugiere categoryHint e isNewCategory=true, categoryId=null.",
                Array.Empty<GravityCategoryItem>());
        }
    }

    public static string BuildCategoryPromptBlock(IReadOnlyList<GravityCategoryItem> categories)
    {
        if (categories.Count == 0)
        {
            return "CATEGORIAS_GRAVITY: (vacío). Debes proponer categoryHint nuevo con isNewCategory=true y categoryId=null.";
        }

        var sb = new StringBuilder();
        sb.AppendLine("CATEGORIAS_GRAVITY (id | nombre). Elige UNA existente si encaja; si no, marca isNewCategory=true:");
        var n = 0;
        foreach (var c in categories
                     .Where(c => !string.IsNullOrWhiteSpace(c.CategoryId) && !string.IsNullOrWhiteSpace(c.Name))
                     .OrderBy(c => c.Name, StringComparer.OrdinalIgnoreCase))
        {
            if (n++ >= MaxCategoriesInPrompt)
            {
                sb.AppendLine($"… (+{categories.Count - MaxCategoriesInPrompt} más omitidas)");
                break;
            }

            sb.Append(c.CategoryId!.Trim());
            sb.Append(" | ");
            sb.AppendLine(c.Name!.Trim());
        }

        sb.AppendLine(
            "Reglas categoría: si reutilizas → isNewCategory=false, categoryId=<id de la lista>, categoryHint=<nombre exacto de la lista>. "
            + "Si creas → isNewCategory=true, categoryId=null, categoryHint=<nombre nuevo en español>.");
        sb.AppendLine(
            "Reglas marca: brandHint siempre no vacío. "
            + "Si reutilizas marca conocida → isNewBrand=false Y brandId obligatorio. "
            + "Si creas → isNewBrand=true y brandId=null. Nunca isNewBrand=false sin brandId.");
        return sb.ToString().TrimEnd();
    }

    /// <summary>
    /// Authoritative post-process: match categoryHint against Gravity list; brandHint via autocomplete.
    /// </summary>
    public static async Task<string> EnrichFichaJsonAsync(
        IServiceProvider services,
        string fichaJson,
        IReadOnlyList<GravityCategoryItem> categories,
        CancellationToken cancellationToken)
    {
        JsonNode? root;
        try
        {
            root = JsonNode.Parse(string.IsNullOrWhiteSpace(fichaJson) ? "{}" : fichaJson);
        }
        catch
        {
            return fichaJson;
        }

        if (root is not JsonObject obj)
            return fichaJson;

        if (obj["error"] is not null)
            return fichaJson;

        var categoryHint = ReadString(obj, "categoryHint");
        var brandHint = ReadString(obj, "brandHint");

        // Category: exact / normalized match against catalog wins over the model flag.
        var catMatch = FindCategory(categories, categoryHint);
        if (catMatch is not null)
        {
            obj["isNewCategory"] = false;
            obj["categoryId"] = catMatch.CategoryId;
            obj["categoryHint"] = catMatch.Name ?? categoryHint;
        }
        else
        {
            obj["isNewCategory"] = true;
            obj["categoryId"] = null;
            if (string.IsNullOrWhiteSpace(categoryHint))
                obj["categoryHint"] = "General";
        }

        // Brand: autocomplete exact name → reuse; else create.
        try
        {
            var db = services.GetRequiredService<ICommerceDbContext>();
            var gravity = services.GetRequiredService<IGravityStoreDataClient>();
            var logger = services.GetRequiredService<ILoggerFactory>().CreateLogger("GenerativeFichaCatalog");
            var workspaceContext = services.GetRequiredService<IWorkspaceContext>();
            var workspaceId = workspaceContext.WorkspaceId
                ?? throw new InvalidOperationException("Workspace is required.");
            var (_, credentials) = await CatalogGravityCredentials.LoadActiveAsync(
                db,
                logger,
                workspaceId,
                integrationId: null,
                cancellationToken);

            var hint = string.IsNullOrWhiteSpace(brandHint) ? "Genérica" : brandHint.Trim();
            obj["brandHint"] = hint;

            GravityBrandItem? brandMatch = null;
            if (!string.IsNullOrWhiteSpace(brandHint))
            {
                var auto = await gravity.AutocompleteBrandsAsync(credentials, hint, cancellationToken);
                var needle = Normalize(hint);
                brandMatch = auto
                    .Where(b => !string.IsNullOrWhiteSpace(b.BrandId) && Normalize(b.Name) == needle)
                    .OrderBy(b => b.BrandId, StringComparer.OrdinalIgnoreCase)
                    .FirstOrDefault();
            }

            if (brandMatch is not null && !string.IsNullOrWhiteSpace(brandMatch.BrandId))
            {
                obj["isNewBrand"] = false;
                obj["brandId"] = brandMatch.BrandId.Trim();
                obj["brandHint"] = string.IsNullOrWhiteSpace(brandMatch.Name) ? hint : brandMatch.Name.Trim();
            }
            else
            {
                obj["isNewBrand"] = true;
                obj["brandId"] = null;
            }
        }
        catch
        {
            obj["isNewBrand"] = true;
            obj["brandId"] = null;
            if (string.IsNullOrWhiteSpace(ReadString(obj, "brandHint")))
                obj["brandHint"] = "Genérica";
        }

        EnforceCreateFlags(obj);
        return obj.ToJsonString(Json);
    }

    /// <summary>
    /// Invariants: reuse requires id; missing id ⇒ create (isNew*=true).
    /// </summary>
    private static void EnforceCreateFlags(JsonObject obj)
    {
        var brandId = ReadString(obj, "brandId");
        if (string.IsNullOrWhiteSpace(brandId))
        {
            obj["isNewBrand"] = true;
            obj["brandId"] = null;
        }
        else
        {
            obj["isNewBrand"] = false;
            obj["brandId"] = brandId;
        }

        var categoryId = ReadString(obj, "categoryId");
        if (string.IsNullOrWhiteSpace(categoryId))
        {
            obj["isNewCategory"] = true;
            obj["categoryId"] = null;
        }
        else
        {
            obj["isNewCategory"] = false;
            obj["categoryId"] = categoryId;
        }

        if (string.IsNullOrWhiteSpace(ReadString(obj, "brandHint")))
            obj["brandHint"] = "Genérica";
        if (string.IsNullOrWhiteSpace(ReadString(obj, "categoryHint")))
            obj["categoryHint"] = "General";
    }

    private static GravityCategoryItem? FindCategory(
        IReadOnlyList<GravityCategoryItem> categories,
        string? hint)
    {
        var needle = Normalize(hint);
        if (needle.Length == 0 || categories.Count == 0)
            return null;

        return categories
            .Where(c => !string.IsNullOrWhiteSpace(c.CategoryId) && Normalize(c.Name) == needle)
            .OrderBy(c => c.CategoryId, StringComparer.OrdinalIgnoreCase)
            .FirstOrDefault();
    }

    private static string Normalize(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return string.Empty;
        var parts = value.Trim()
            .Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        return string.Join(' ', parts).ToLowerInvariant();
    }

    private static string? ReadString(JsonObject obj, string name)
    {
        if (!obj.TryGetPropertyValue(name, out var node) || node is null)
            return null;
        if (node is JsonValue value && value.TryGetValue<string>(out var s))
            return string.IsNullOrWhiteSpace(s) ? null : s.Trim();
        return null;
    }
}