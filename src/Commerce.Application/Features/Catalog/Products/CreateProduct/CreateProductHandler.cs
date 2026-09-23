using Microsoft.Extensions.Logging;
using Commerce.Application.Abstracts;
using Commerce.Application.Common;
using Commerce.Application.Common.Abstracts;

namespace Commerce.Application.Features.Catalog.Products.CreateProduct;

public sealed class CreateProductHandler(
    ICommerceDbContext db,
    IWorkspaceContext workspaceContext,
    IGravityStoreDataClient gravityStore,
    ILogger<CreateProductHandler> logger)
    : ICommandHandler<CreateProductCommand, CreateProductResult>
{
    public async Task<CreateProductResult> Handle(CreateProductCommand request, CancellationToken cancellationToken)
    {
        var workspaceId = await WorkspaceRequirement.RequireWorkspaceIdAsync(
            workspaceContext, db, cancellationToken);
        var (_, credentials) = await CatalogGravityCredentials.LoadActiveAsync(
            db,
            logger,
            workspaceId,
            request.IntegrationId,
            cancellationToken);

        var brandId = string.IsNullOrWhiteSpace(request.BrandId) ? null : request.BrandId.Trim();
        var brandName = string.IsNullOrWhiteSpace(request.BrandName) ? null : request.BrandName.Trim();
        var categoryId = string.IsNullOrWhiteSpace(request.CategoryId) ? null : request.CategoryId.Trim();
        var categoryPath = string.IsNullOrWhiteSpace(request.CategoryPath) ? null : request.CategoryPath.Trim();

        (brandId, brandName) = await ResolveBrandAsync(
            credentials,
            brandId,
            brandName,
            cancellationToken);

        (categoryId, categoryPath) = await ResolveCategoryAsync(
            credentials,
            categoryId,
            categoryPath,
            cancellationToken);

        var created = await CreateProductWithCategoryRetryAsync(
            credentials,
            ToRequest(request, brandId, brandName, categoryId, categoryPath),
            cancellationToken);

        return new CreateProductResult(created.ProductId, created.Name);
    }

    /// <summary>
    /// Gravity CreateProduct can 409 "Categoria … invalido" until CategoryCacheService catches up
    /// even when Categories/all already lists the id. Retry with backoff.
    /// </summary>
    private async Task<GravityProductWriteResult> CreateProductWithCategoryRetryAsync(
        GravityStoreCredentials credentials,
        GravityProductCreateRequest productRequest,
        CancellationToken cancellationToken)
    {
        const int maxAttempts = 8;
        InvalidOperationException? last = null;

        for (var attempt = 1; attempt <= maxAttempts; attempt++)
        {
            try
            {
                return await gravityStore.CreateProductAsync(credentials, productRequest, cancellationToken);
            }
            catch (InvalidOperationException ex) when (IsInvalidCategoryError(ex) && attempt < maxAttempts)
            {
                last = ex;
                var delayMs = 500 * attempt * attempt; // 0.5s, 2s, 4.5s, …
                logger.LogWarning(
                    "CreateProduct: Gravity rejected CategoryId={CategoryId} (attempt {Attempt}/{Max}). Waiting {DelayMs}ms. {Error}",
                    productRequest.CategoryId,
                    attempt,
                    maxAttempts,
                    delayMs,
                    ex.Message);
                await Task.Delay(TimeSpan.FromMilliseconds(delayMs), cancellationToken);
            }
        }

        throw last ?? new InvalidOperationException(
            "Gravity rechazó la categoría al crear el producto. Elige una categoría del árbol (ya indexada) e inténtalo de nuevo.");
    }

    internal static bool IsInvalidCategoryError(InvalidOperationException ex)
    {
        var msg = ex.Message ?? string.Empty;
        return msg.Contains("(409)", StringComparison.Ordinal)
               && (msg.Contains("Categoria", StringComparison.OrdinalIgnoreCase)
                   || msg.Contains("Categoría", StringComparison.OrdinalIgnoreCase)
                   || msg.Contains("Category", StringComparison.OrdinalIgnoreCase))
               && msg.Contains("invalido", StringComparison.OrdinalIgnoreCase);
    }

    private async Task<(string BrandId, string? BrandName)> ResolveBrandAsync(
        GravityStoreCredentials credentials,
        string? brandId,
        string? brandName,
        CancellationToken cancellationToken)
    {
        if (!string.IsNullOrWhiteSpace(brandId))
        {
            var byId = await gravityStore.GetBrandAsync(credentials, brandId, cancellationToken);
            if (byId is not null && !string.IsNullOrWhiteSpace(byId.BrandId))
            {
                logger.LogInformation(
                    "CreateProduct: brand resolved by id. BrandId={BrandId} Name={Name}",
                    byId.BrandId,
                    byId.Name);
                return (byId.BrandId, byId.Name ?? brandName);
            }

            // Stale/unknown id — fall through to name resolve when possible.
            if (string.IsNullOrWhiteSpace(brandName))
            {
                throw new InvalidOperationException(
                    $"La marca «{brandId}» no existe en Gravity. Indica BrandName para crearla o elige una existente.");
            }
        }

        if (string.IsNullOrWhiteSpace(brandName))
        {
            throw new InvalidOperationException(
                "La marca es obligatoria: elige una marca existente o indica un nombre para crearla.");
        }

        var match = await FindBrandByAutocompleteAsync(credentials, brandName, cancellationToken);
        if (match is not null)
            return (match.BrandId, match.Name ?? brandName);

        var createdBrand = await gravityStore.CreateBrandAsync(
            credentials,
            new GravityBrandCreateRequest(brandName),
            cancellationToken);
        if (string.IsNullOrWhiteSpace(createdBrand.BrandId))
        {
            throw new InvalidOperationException(
                $"No se pudo obtener BrandId al crear la marca '{brandName}'.");
        }

        // Confirm with GET; re-check autocomplete in case Gravity already had the same name.
        var confirmed = await gravityStore.GetBrandAsync(credentials, createdBrand.BrandId, cancellationToken);
        var afterCreate = await FindBrandByAutocompleteAsync(credentials, brandName, cancellationToken);
        if (afterCreate is not null
            && !string.Equals(afterCreate.BrandId, createdBrand.BrandId, StringComparison.OrdinalIgnoreCase))
        {
            logger.LogWarning(
                "CreateProduct: brand name '{Name}' already existed after create. Using existing BrandId={ExistingId} (created={CreatedId})",
                brandName,
                afterCreate.BrandId,
                createdBrand.BrandId);
            return (afterCreate.BrandId, afterCreate.Name ?? brandName);
        }

        var resolved = confirmed ?? createdBrand;
        logger.LogInformation(
            "CreateProduct: brand created. BrandId={BrandId} Name={Name}",
            resolved.BrandId,
            resolved.Name ?? brandName);
        return (resolved.BrandId, resolved.Name ?? brandName);
    }

    /// <summary>
    /// Exact name match via Gravity Brands/autocomplete only. Prefer stable id if duplicates exist.
    /// </summary>
    private async Task<GravityBrandItem?> FindBrandByAutocompleteAsync(
        GravityStoreCredentials credentials,
        string brandName,
        CancellationToken cancellationToken)
    {
        var needle = NormalizeLabel(brandName);
        if (needle.Length == 0)
            return null;

        var auto = await gravityStore.AutocompleteBrandsAsync(credentials, brandName, cancellationToken);
        var exact = auto
            .Where(b => !string.IsNullOrWhiteSpace(b.BrandId)
                        && NormalizeLabel(b.Name) == needle)
            .GroupBy(b => b.BrandId, StringComparer.OrdinalIgnoreCase)
            .Select(g => g.First())
            .OrderBy(b => b.BrandId, StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (exact.Count == 0)
            return null;

        if (exact.Count > 1)
        {
            logger.LogWarning(
                "CreateProduct: autocomplete returned {Count} brands named '{Name}'; using BrandId={BrandId}",
                exact.Count,
                brandName,
                exact[0].BrandId);
        }
        else
        {
            logger.LogInformation(
                "CreateProduct: brand matched via autocomplete. BrandId={BrandId} Name={Name}",
                exact[0].BrandId,
                exact[0].Name);
        }

        return exact[0];
    }

    /// <summary>
    /// Resolve CategoryId against Gravity Categories/all only (same cache CreateProduct uses).
    /// Never trust GetCategory alone — ids not yet in Categories/all cause 409 on Products.
    /// </summary>
    private async Task<(string CategoryId, string? CategoryPath)> ResolveCategoryAsync(
        GravityStoreCredentials credentials,
        string? categoryId,
        string? categoryPath,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(categoryId) && string.IsNullOrWhiteSpace(categoryPath))
        {
            throw new InvalidOperationException(
                "La categoría es obligatoria: elige una del árbol o indica un nombre para crearla.");
        }

        var categories = await gravityStore.ListCategoriesAsync(credentials, name: null, cancellationToken);
        GravityCategoryItem? match = null;

        if (!string.IsNullOrWhiteSpace(categoryId))
        {
            match = categories.FirstOrDefault(c =>
                string.Equals(c.CategoryId, categoryId, StringComparison.OrdinalIgnoreCase));
            if (match is null)
            {
                await WaitUntilCategoryVisibleInListAsync(
                    credentials,
                    categoryId,
                    categoryPath ?? categoryId,
                    requireInList: true,
                    cancellationToken);
                categories = await gravityStore.ListCategoriesAsync(credentials, name: null, cancellationToken);
                match = categories.FirstOrDefault(c =>
                    string.Equals(c.CategoryId, categoryId, StringComparison.OrdinalIgnoreCase));
            }
        }

        var leafName = !string.IsNullOrWhiteSpace(categoryPath) ? LeafCategoryName(categoryPath) : null;

        if (match is null && !string.IsNullOrWhiteSpace(leafName))
            match = FindCategoryByExactName(categories, leafName);

        if (match is not null && !string.IsNullOrWhiteSpace(match.CategoryId))
        {
            var byName = FindAllCategoriesByExactName(categories, match.Name ?? leafName ?? match.CategoryId);
            if (byName.Count > 1)
            {
                match = byName.OrderBy(c => c.CategoryId, StringComparer.OrdinalIgnoreCase).First();
                logger.LogWarning(
                    "CreateProduct: multiple categories named '{Name}'; using oldest CategoryId={CategoryId}",
                    match.Name,
                    match.CategoryId);
            }

            logger.LogInformation(
                "CreateProduct: category resolved from Categories/all. CategoryId={CategoryId} Name={Name}",
                match.CategoryId,
                match.Name);
            return (match.CategoryId, match.Name ?? categoryPath);
        }

        if (string.IsNullOrWhiteSpace(leafName))
        {
            throw new InvalidOperationException(
                $"La categoría «{categoryId}» no está indexada en Gravity para productos. "
                + "Elige una del árbol de catálogo o espera unos segundos e inténtalo de nuevo.");
        }

        var indexedBeforeCreate = categories;

        categories = await gravityStore.ListCategoriesAsync(credentials, name: null, cancellationToken);
        match = FindCategoryByExactName(categories, leafName);
        if (match is not null && !string.IsNullOrWhiteSpace(match.CategoryId))
        {
            logger.LogInformation(
                "CreateProduct: category matched on re-list before create. CategoryId={CategoryId} Name={Name}",
                match.CategoryId,
                match.Name);
            return (match.CategoryId, match.Name ?? leafName);
        }

        var parentId = ResolveParentIdFromPath(indexedBeforeCreate, categoryPath);
        var created = await gravityStore.CreateCategoryAsync(
            credentials,
            new GravityCategoryCreateRequest(leafName, ParentCategoryId: parentId),
            cancellationToken);
        if (string.IsNullOrWhiteSpace(created.CategoryId))
        {
            throw new InvalidOperationException(
                $"No se pudo obtener CategoryId al crear la categoría '{leafName}'.");
        }

        await WaitUntilCategoryVisibleInListAsync(
            credentials,
            created.CategoryId,
            leafName,
            requireInList: true,
            cancellationToken);

        categories = await gravityStore.ListCategoriesAsync(credentials, name: null, cancellationToken);
        var byNameAfter = FindAllCategoriesByExactName(categories, leafName);
        if (byNameAfter.Count > 0)
        {
            var preExisting = byNameAfter
                .Where(c => indexedBeforeCreate.Any(x =>
                    string.Equals(x.CategoryId, c.CategoryId, StringComparison.OrdinalIgnoreCase)))
                .OrderBy(c => c.CategoryId, StringComparer.OrdinalIgnoreCase)
                .FirstOrDefault();
            if (preExisting is not null)
            {
                logger.LogWarning(
                    "CreateProduct: using pre-indexed category '{Name}' CategoryId={CategoryId} instead of just-created {CreatedId}",
                    preExisting.Name,
                    preExisting.CategoryId,
                    created.CategoryId);
                return (preExisting.CategoryId, preExisting.Name ?? leafName);
            }

            var preferred = byNameAfter
                .OrderBy(c => c.CategoryId, StringComparer.OrdinalIgnoreCase)
                .First();
            logger.LogInformation(
                "CreateProduct: category created and indexed. CategoryId={CategoryId} Name={Name}",
                preferred.CategoryId,
                preferred.Name ?? leafName);
            return (preferred.CategoryId, preferred.Name ?? leafName);
        }

        throw new InvalidOperationException(
            $"Se creó la categoría «{leafName}» (id {created.CategoryId}) pero Gravity aún no la indexa para productos. "
            + "Elige una categoría del árbol e inténtalo de nuevo en unos segundos.");
    }

    private static string NormalizeLabel(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return string.Empty;
        var trimmed = value.Trim();
        var chars = trimmed.Where(c => !char.IsWhiteSpace(c) || c == ' ').ToArray();
        var collapsed = string.Join(
            ' ',
            new string(chars).Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries));
        return collapsed.ToLowerInvariant();
    }

    private static GravityCategoryItem? FindCategoryByExactName(
        IReadOnlyList<GravityCategoryItem> categories,
        string name)
    {
        var all = FindAllCategoriesByExactName(categories, name);
        return all.Count == 0
            ? null
            : all.OrderBy(c => c.CategoryId, StringComparer.OrdinalIgnoreCase).First();
    }

    private static IReadOnlyList<GravityCategoryItem> FindAllCategoriesByExactName(
        IReadOnlyList<GravityCategoryItem> categories,
        string name)
    {
        var needle = NormalizeLabel(name);
        if (needle.Length == 0 || categories.Count == 0)
            return [];

        return categories
            .Where(c => !string.IsNullOrWhiteSpace(c.CategoryId) && NormalizeLabel(c.Name) == needle)
            .GroupBy(c => c.CategoryId, StringComparer.OrdinalIgnoreCase)
            .Select(g => g.First())
            .ToList();
    }

    /// <summary>
    /// For "Calzado > Outdoor > Calzado de Outdoor", attach the new leaf under "Outdoor" when that node exists.
    /// </summary>
    internal static string? ResolveParentIdFromPath(
        IReadOnlyList<GravityCategoryItem> categories,
        string? categoryPath)
    {
        if (string.IsNullOrWhiteSpace(categoryPath) || categories.Count == 0)
            return null;

        var parts = categoryPath
            .Split('>', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length < 2)
            return null;

        var parentName = parts[^2];
        return FindCategoryByExactName(categories, parentName)?.CategoryId;
    }

    /// <summary>
    /// Gravity CreateProduct validates against CategoryCacheService; newly POSTed categories may lag.
    /// Wait until id appears in Categories/all (required when requireInList).
    /// </summary>
    private async Task WaitUntilCategoryVisibleInListAsync(
        GravityStoreCredentials credentials,
        string categoryId,
        string categoryName,
        bool requireInList,
        CancellationToken cancellationToken)
    {
        const int attempts = 20;
        for (var i = 0; i < attempts; i++)
        {
            var list = await gravityStore.ListCategoriesAsync(credentials, name: null, cancellationToken);
            var byId = list.Any(c => string.Equals(c.CategoryId, categoryId, StringComparison.OrdinalIgnoreCase));
            if (byId)
                return;

            if (i < attempts - 1)
                await Task.Delay(TimeSpan.FromMilliseconds(750), cancellationToken);
        }

        if (requireInList)
        {
            throw new InvalidOperationException(
                $"La categoría «{categoryName}» (id {categoryId}) aún no aparece en Categories/all de Gravity. "
                + "Elige una del árbol de catálogo o espera e inténtalo de nuevo.");
        }

        logger.LogWarning(
            "CreateProduct: category {CategoryId} ('{Name}') not yet in Categories/all after wait.",
            categoryId,
            categoryName);
    }

    internal static GravityProductCreateRequest ToRequest(
        CreateProductCommand request,
        string brandId,
        string? brandName,
        string categoryId,
        string? categoryPath)
    {
        IReadOnlyList<GravityProductVariationInput>? variations = null;
        if (request.Variations is { Count: > 0 })
        {
            variations = request.Variations
                .Select(v => new GravityProductVariationInput(
                    string.IsNullOrWhiteSpace(v.Name) ? request.Name.Trim() : v.Name.Trim(),
                    string.IsNullOrWhiteSpace(v.Sku) ? null : v.Sku.Trim(),
                    string.IsNullOrWhiteSpace(v.VariantName) ? null : v.VariantName.Trim(),
                    v.BasePrice,
                    v.Stock,
                    string.IsNullOrWhiteSpace(v.ImageUrl) ? null : v.ImageUrl.Trim(),
                    v.Options is { Count: > 0 }
                        ? v.Options
                            .Where(o => !string.IsNullOrWhiteSpace(o.Key) && !string.IsNullOrWhiteSpace(o.Name))
                            .Select(o => new GravityProductVariationOption(
                                o.Key.Trim(),
                                o.Name.Trim(),
                                string.IsNullOrWhiteSpace(o.Value) ? null : o.Value.Trim()))
                            .ToArray()
                        : null))
                .ToArray();
        }

        return new(
            request.Name.Trim(),
            request.Description.Trim(),
            brandId,
            brandName,
            categoryId,
            categoryPath,
            string.IsNullOrWhiteSpace(request.ImageUrl) ? null : request.ImageUrl.Trim(),
            request.IsActive,
            request.ShowInCatalog,
            SellerId: null,
            Variations: variations);
    }

    /// <summary>
    /// From "Calzado > Trekking" or a plain hint, use the leaf label as the lookup name.
    /// </summary>
    internal static string LeafCategoryName(string pathOrName)
    {
        var parts = pathOrName
            .Split('>', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
        return parts.Length > 0 ? parts[^1] : pathOrName.Trim();
    }
}
