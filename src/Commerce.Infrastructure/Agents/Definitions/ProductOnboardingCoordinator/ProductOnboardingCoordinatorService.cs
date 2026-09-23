using Commerce.Application.Abstracts;
using Commerce.Application.Features.CreateProduct;
using Commerce.Infrastructure.Vectors.Common;
using Microsoft.Extensions.Logging;

namespace Commerce.Infrastructure.Agents.Definitions.ProductOnboardingCoordinator;

public sealed class ProductOnboardingCoordinatorService(
    IGravityStoreDataClient gravity,
    IImageVerbalizer verbalizer,
    ILogger<ProductOnboardingCoordinatorService> logger) : IProductOnboardingCoordinator
{
    public async Task<OnboardingDraft> AnalyzeAsync(
        GravityStoreCredentials credentials,
        string? title,
        string? imageUrl,
        Stream? imageStream,
        string? imageContentType,
        CancellationToken cancellationToken = default)
    {
        string? brandName = null;
        string? categoryName = null;
        string? productTitle = string.IsNullOrWhiteSpace(title) ? null : title.Trim();

        if (imageStream is not null)
        {
            var analysis = await verbalizer.AnalyzeProductAsync(
                imageStream,
                imageContentType,
                productTitle,
                cancellationToken);
            ApplyAnalysis(analysis, ref brandName, ref categoryName, ref productTitle);
        }
        else if (!string.IsNullOrWhiteSpace(imageUrl))
        {
            var analysis = await verbalizer.AnalyzeProductUrlAsync(imageUrl, productTitle, cancellationToken);
            ApplyAnalysis(analysis, ref brandName, ref categoryName, ref productTitle);
        }

        productTitle ??= "Producto sin nombre";
        brandName = string.IsNullOrWhiteSpace(brandName) ? "Genérica" : brandName.Trim();
        categoryName = string.IsNullOrWhiteSpace(categoryName) ? "General" : categoryName.Trim();

        var brands = await gravity.ListBrandsAsync(credentials, brandName, page: 1, pageSize: 20, cancellationToken);
        var brandMatch = brands.FirstOrDefault(b =>
            string.Equals(b.Name, brandName, StringComparison.OrdinalIgnoreCase));
        if (brandMatch is null && brands.Count > 0)
        {
            var auto = await gravity.AutocompleteBrandsAsync(credentials, brandName, cancellationToken);
            brandMatch = auto.FirstOrDefault(b =>
                string.Equals(b.Name, brandName, StringComparison.OrdinalIgnoreCase));
        }

        var categories = await gravity.ListCategoriesAsync(credentials, categoryName, cancellationToken);
        var categoryMatch = categories.FirstOrDefault(c =>
            string.Equals(c.Name, categoryName, StringComparison.OrdinalIgnoreCase));

        logger.LogInformation(
            "Onboarding analyze draft. Title={Title} Brand={Brand} BrandNew={BrandNew} Category={Category} CategoryNew={CategoryNew}",
            productTitle,
            brandName,
            brandMatch is null,
            categoryName,
            categoryMatch is null);

        return new OnboardingDraft(
            new OnboardingDraftBrand(brandName, brandMatch is null, brandMatch?.BrandId),
            new OnboardingDraftCategory(categoryName, categoryMatch is null, categoryMatch?.CategoryId),
            new OnboardingDraftProduct(productTitle, imageUrl, productTitle));
    }

    public async Task<OnboardingCreateResult> CreateApprovedAsync(
        GravityStoreCredentials credentials,
        string workflowId,
        OnboardingDraft draft,
        OnboardingApprovals approvals,
        CancellationToken cancellationToken = default)
    {
        if (!approvals.Product)
            throw new InvalidOperationException("Product approval is required to create.");

        string? brandId = draft.Brand.Id;
        string? brandName = draft.Brand.Name;
        if (draft.Brand.IsNew)
        {
            if (!approvals.Brand)
                throw new InvalidOperationException("Brand is new and was not approved.");

            var created = await gravity.CreateBrandAsync(
                credentials,
                new GravityBrandCreateRequest(draft.Brand.Name),
                cancellationToken);
            brandId = created.BrandId;
            brandName = created.Name ?? draft.Brand.Name;
        }
        else if (string.IsNullOrWhiteSpace(brandId))
        {
            throw new InvalidOperationException("Existing brand id is missing from draft.");
        }

        string? categoryId = draft.Category.Id;
        string? categoryName = draft.Category.Name;
        if (draft.Category.IsNew)
        {
            if (!approvals.Category)
                throw new InvalidOperationException("Category is new and was not approved.");

            var created = await gravity.CreateCategoryAsync(
                credentials,
                new GravityCategoryCreateRequest(draft.Category.Name),
                cancellationToken);
            categoryId = created.CategoryId;
            categoryName = created.Name ?? draft.Category.Name;
        }
        else if (string.IsNullOrWhiteSpace(categoryId))
        {
            throw new InvalidOperationException("Existing category id is missing from draft.");
        }

        var product = await gravity.CreateProductAsync(
            credentials,
            new GravityProductCreateRequest(
                draft.Product.Title,
                draft.Product.Description ?? draft.Product.Title,
                brandId,
                brandName,
                categoryId,
                categoryName,
                draft.Product.ImageUrl),
            cancellationToken);

        logger.LogInformation(
            "Onboarding created product. WorkflowId={WorkflowId} ProductId={ProductId} BrandId={BrandId} CategoryId={CategoryId}",
            workflowId,
            product.ProductId,
            brandId,
            categoryId);

        return new OnboardingCreateResult(workflowId, brandId, categoryId, product.ProductId);
    }

    private static void ApplyAnalysis(
        ProductImageAnalysis? analysis,
        ref string? brandName,
        ref string? categoryName,
        ref string? productTitle)
    {
        if (analysis is null)
            return;

        if (!string.IsNullOrWhiteSpace(analysis.Brand))
            brandName = analysis.Brand.Trim();
        if (!string.IsNullOrWhiteSpace(analysis.Category))
            categoryName = analysis.Category.Trim();
        if (string.IsNullOrWhiteSpace(productTitle) && !string.IsNullOrWhiteSpace(analysis.Title))
            productTitle = analysis.Title.Trim();
    }
}
