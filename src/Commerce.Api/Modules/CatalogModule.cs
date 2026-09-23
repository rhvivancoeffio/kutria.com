using Carter;
using FluentValidation;
using MediatR;
using Commerce.Application.Features.Catalog.Attributes.CreateAttribute;
using Commerce.Application.Features.Catalog.Attributes.DeleteAttribute;
using Commerce.Application.Features.Catalog.Attributes.GetAttribute;
using Commerce.Application.Features.Catalog.Attributes.ListAttributes;
using Commerce.Application.Features.Catalog.Attributes.UpdateAttribute;
using Commerce.Application.Features.Catalog.Brands.AutocompleteBrands;
using Commerce.Application.Features.Catalog.Brands.CreateBrand;
using Commerce.Application.Features.Catalog.Brands.DeleteBrand;
using Commerce.Application.Features.Catalog.Brands.GetBrand;
using Commerce.Application.Features.Catalog.Brands.ListBrands;
using Commerce.Application.Features.Catalog.Brands.UpdateBrand;
using Commerce.Application.Features.Catalog.Categories.CreateCategory;
using Commerce.Application.Features.Catalog.Categories.DeleteCategory;
using Commerce.Application.Features.Catalog.Categories.GetCategory;
using Commerce.Application.Features.Catalog.Categories.ListCategories;
using Commerce.Application.Features.Catalog.Categories.UpdateCategory;
using Commerce.Application.Features.Catalog.Products.CreateProduct;
using Commerce.Application.Features.Catalog.Products.DeleteProduct;
using Commerce.Application.Features.Catalog.Products.GetProduct;
using Commerce.Application.Features.Catalog.Products.ListProducts;
using Commerce.Application.Features.Catalog.Products.UpdateProduct;

namespace Commerce.Api.Modules;

public sealed class CatalogModule : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        Map(app.MapGroup("/t/{__tenant__}/catalog").WithTags("Catalog"));
    }

    private static void Map(RouteGroupBuilder group)
    {
        group.MapGet("/brands", async (
            string? name,
            int? page,
            int? pageSize,
            Guid? integrationId,
            IMediator mediator,
            CancellationToken ct) =>
            await Send(mediator, new ListBrandsQuery(name, page ?? 1, pageSize ?? 20, integrationId), ct));

        group.MapGet("/brands/autocomplete", async (
            string? name,
            Guid? integrationId,
            IMediator mediator,
            CancellationToken ct) =>
            await Send(mediator, new AutocompleteBrandsQuery(name ?? string.Empty, integrationId), ct));

        group.MapGet("/brands/{brandId}", async (
            string brandId,
            Guid? integrationId,
            IMediator mediator,
            CancellationToken ct) =>
        {
            try
            {
                var result = await mediator.Send(new GetBrandQuery(brandId, integrationId), ct);
                return result is null ? Results.NotFound() : Results.Ok(result);
            }
            catch (ValidationException ex)
            {
                var message = ex.Errors.FirstOrDefault()?.ErrorMessage ?? "Invalid data.";
                return Results.BadRequest(new { error = message });
            }
            catch (InvalidOperationException ex)
            {
                return Results.BadRequest(new { error = ex.Message });
            }
        });

        group.MapPost("/brands", async (CreateBrandRequest body, IMediator mediator, CancellationToken ct) =>
            await Send(mediator, new CreateBrandCommand(
                body.Name,
                body.Description,
                body.IsActive ?? true,
                body.ImageUrl,
                body.IntegrationId), ct));

        group.MapPut("/brands/{brandId}", async (
            string brandId,
            UpdateBrandRequest body,
            IMediator mediator,
            CancellationToken ct) =>
            await Send(mediator, new UpdateBrandCommand(
                brandId,
                body.Name,
                body.Description,
                body.IsActive ?? true,
                body.ImageUrl,
                body.IntegrationId), ct));

        group.MapDelete("/brands/{brandId}", async (
            string brandId,
            Guid? integrationId,
            IMediator mediator,
            CancellationToken ct) =>
            await Send(mediator, new DeleteBrandCommand(brandId, integrationId), ct));

        group.MapGet("/attributes", async (
            string? name,
            int? page,
            int? pageSize,
            Guid? integrationId,
            IMediator mediator,
            CancellationToken ct) =>
            await Send(mediator, new ListAttributesQuery(name, page ?? 1, pageSize ?? 20, integrationId), ct));

        group.MapGet("/attributes/{entityAttributeId}", async (
            string entityAttributeId,
            Guid? integrationId,
            IMediator mediator,
            CancellationToken ct) =>
        {
            try
            {
                var result = await mediator.Send(new GetAttributeQuery(entityAttributeId, integrationId), ct);
                return result is null ? Results.NotFound() : Results.Ok(result);
            }
            catch (ValidationException ex)
            {
                var message = ex.Errors.FirstOrDefault()?.ErrorMessage ?? "Invalid data.";
                return Results.BadRequest(new { error = message });
            }
            catch (InvalidOperationException ex)
            {
                return Results.BadRequest(new { error = ex.Message });
            }
        });

        group.MapPost("/attributes", async (CreateAttributeRequest body, IMediator mediator, CancellationToken ct) =>
            await Send(mediator, new CreateAttributeCommand(
                body.EntityName,
                body.Name,
                body.IsMultiOption,
                body.SpecificationType,
                body.Required,
                body.Description,
                body.Label,
                body.Values,
                body.Order ?? 0,
                body.IsPublic ?? false,
                body.SectionGroup,
                body.IntegrationId), ct));

        group.MapPut("/attributes/{entityAttributeId}", async (
            string entityAttributeId,
            UpdateAttributeRequest body,
            IMediator mediator,
            CancellationToken ct) =>
            await Send(mediator, new UpdateAttributeCommand(
                entityAttributeId,
                body.EntityName,
                body.Name,
                body.IsMultiOption,
                body.SpecificationType,
                body.Required,
                body.Description,
                body.Label,
                body.Values,
                body.Order ?? 0,
                body.IsPublic ?? false,
                body.SectionGroup,
                body.IntegrationId), ct));

        group.MapDelete("/attributes/{entityAttributeId}", async (
            string entityAttributeId,
            Guid? integrationId,
            IMediator mediator,
            CancellationToken ct) =>
            await Send(mediator, new DeleteAttributeCommand(entityAttributeId, integrationId), ct));

        group.MapGet("/categories", async (
            string? name,
            Guid? integrationId,
            IMediator mediator,
            CancellationToken ct) =>
            await Send(mediator, new ListCategoriesQuery(name, integrationId), ct));

        group.MapGet("/categories/{categoryId}", async (
            string categoryId,
            Guid? integrationId,
            IMediator mediator,
            CancellationToken ct) =>
        {
            try
            {
                var result = await mediator.Send(new GetCategoryQuery(categoryId, integrationId), ct);
                return result is null ? Results.NotFound() : Results.Ok(result);
            }
            catch (ValidationException ex)
            {
                var message = ex.Errors.FirstOrDefault()?.ErrorMessage ?? "Invalid data.";
                return Results.BadRequest(new { error = message });
            }
            catch (InvalidOperationException ex)
            {
                return Results.BadRequest(new { error = ex.Message });
            }
        });

        group.MapPost("/categories", async (CreateCategoryRequest body, IMediator mediator, CancellationToken ct) =>
            await Send(mediator, new CreateCategoryCommand(
                body.Name,
                body.Slug,
                body.Description,
                body.ParentCategoryId,
                body.IsActive ?? true,
                body.IntegrationId), ct));

        group.MapPut("/categories/{categoryId}", async (
            string categoryId,
            UpdateCategoryRequest body,
            IMediator mediator,
            CancellationToken ct) =>
            await Send(mediator, new UpdateCategoryCommand(
                categoryId,
                body.Name,
                body.Slug,
                body.Description,
                body.ParentCategoryId,
                body.IsActive ?? true,
                body.IntegrationId), ct));

        group.MapDelete("/categories/{categoryId}", async (
            string categoryId,
            Guid? integrationId,
            IMediator mediator,
            CancellationToken ct) =>
            await Send(mediator, new DeleteCategoryCommand(categoryId, integrationId), ct));

        group.MapGet("/products", async (
            int? page,
            int? pageSize,
            Guid? integrationId,
            IMediator mediator,
            CancellationToken ct) =>
            await Send(mediator, new ListProductsQuery(page ?? 1, pageSize ?? 20, integrationId), ct));

        group.MapGet("/products/{productId}", async (
            string productId,
            Guid? integrationId,
            IMediator mediator,
            CancellationToken ct) =>
        {
            try
            {
                var result = await mediator.Send(new GetProductQuery(productId, integrationId), ct);
                return result is null ? Results.NotFound() : Results.Ok(result);
            }
            catch (ValidationException ex)
            {
                var message = ex.Errors.FirstOrDefault()?.ErrorMessage ?? "Invalid data.";
                return Results.BadRequest(new { error = message });
            }
            catch (InvalidOperationException ex)
            {
                return Results.BadRequest(new { error = ex.Message });
            }
        });

        group.MapPost("/products", async (CreateCatalogProductRequest body, IMediator mediator, CancellationToken ct) =>
            await Send(mediator, new CreateProductCommand(
                body.Name,
                body.Description,
                body.BrandId,
                body.BrandName,
                body.CategoryId,
                body.CategoryPath,
                body.ImageUrl,
                body.IsActive ?? true,
                body.ShowInCatalog ?? true,
                body.IntegrationId,
                MapVariations(body.Variations)), ct));

        group.MapPut("/products/{productId}", async (
            string productId,
            UpdateCatalogProductRequest body,
            IMediator mediator,
            CancellationToken ct) =>
            await Send(mediator, new UpdateProductCommand(
                productId,
                body.Name,
                body.Description,
                body.BrandId,
                body.BrandName,
                body.CategoryId,
                body.CategoryPath,
                body.ImageUrl,
                body.IsActive ?? true,
                body.ShowInCatalog ?? true,
                body.IntegrationId), ct));

        group.MapDelete("/products/{productId}", async (
            string productId,
            Guid? integrationId,
            IMediator mediator,
            CancellationToken ct) =>
            await Send(mediator, new DeleteProductCommand(productId, integrationId), ct));
    }

    private static async Task<IResult> Send<T>(IMediator mediator, IRequest<T> request, CancellationToken ct)
    {
        try
        {
            return Results.Ok(await mediator.Send(request, ct));
        }
        catch (ValidationException ex)
        {
            var message = ex.Errors.FirstOrDefault()?.ErrorMessage ?? "Invalid data.";
            return Results.BadRequest(new { error = message });
        }
        catch (KeyNotFoundException ex)
        {
            return Results.NotFound(new { error = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return Results.BadRequest(new { error = ex.Message });
        }
    }

    private static IReadOnlyList<CreateProductVariationInput>? MapVariations(
        IReadOnlyList<CreateCatalogProductVariationRequest>? variations)
    {
        if (variations is null || variations.Count == 0)
            return null;

        return variations
            .Select(v => new CreateProductVariationInput(
                v.Name ?? "",
                v.Sku,
                v.VariantName,
                v.BasePrice ?? 0,
                v.Stock ?? 0,
                v.ImageUrl,
                v.Options is { Count: > 0 }
                    ? v.Options
                        .Select(o => new CreateProductVariationOptionInput(o.Key ?? "", o.Name ?? "", o.Value))
                        .ToArray()
                    : null))
            .ToArray();
    }

    private sealed record CreateBrandRequest(
        string Name,
        string? Description = null,
        bool? IsActive = true,
        string? ImageUrl = null,
        Guid? IntegrationId = null);

    private sealed record UpdateBrandRequest(
        string Name,
        string? Description = null,
        bool? IsActive = true,
        string? ImageUrl = null,
        Guid? IntegrationId = null);

    private sealed record CreateAttributeRequest(
        string EntityName,
        string Name,
        bool IsMultiOption,
        int SpecificationType,
        bool Required,
        string? Description = null,
        string? Label = null,
        string? Values = null,
        int? Order = 0,
        bool? IsPublic = false,
        string? SectionGroup = null,
        Guid? IntegrationId = null);

    private sealed record UpdateAttributeRequest(
        string EntityName,
        string Name,
        bool IsMultiOption,
        int SpecificationType,
        bool Required,
        string? Description = null,
        string? Label = null,
        string? Values = null,
        int? Order = 0,
        bool? IsPublic = false,
        string? SectionGroup = null,
        Guid? IntegrationId = null);

    private sealed record CreateCategoryRequest(
        string Name,
        string? Slug = null,
        string? Description = null,
        string? ParentCategoryId = null,
        bool? IsActive = true,
        Guid? IntegrationId = null);

    private sealed record UpdateCategoryRequest(
        string Name,
        string? Slug = null,
        string? Description = null,
        string? ParentCategoryId = null,
        bool? IsActive = true,
        Guid? IntegrationId = null);

    private sealed record CreateCatalogProductRequest(
        string Name,
        string Description,
        string? BrandId = null,
        string? BrandName = null,
        string? CategoryId = null,
        string? CategoryPath = null,
        string? ImageUrl = null,
        bool? IsActive = true,
        bool? ShowInCatalog = true,
        Guid? IntegrationId = null,
        IReadOnlyList<CreateCatalogProductVariationRequest>? Variations = null);

    private sealed record CreateCatalogProductVariationRequest(
        string? Name = null,
        string? Sku = null,
        string? VariantName = null,
        decimal? BasePrice = null,
        int? Stock = null,
        string? ImageUrl = null,
        IReadOnlyList<CreateCatalogProductVariationOptionRequest>? Options = null);

    private sealed record CreateCatalogProductVariationOptionRequest(
        string? Key = null,
        string? Name = null,
        string? Value = null);

    private sealed record UpdateCatalogProductRequest(
        string Name,
        string Description,
        string? BrandId = null,
        string? BrandName = null,
        string? CategoryId = null,
        string? CategoryPath = null,
        string? ImageUrl = null,
        bool? IsActive = true,
        bool? ShowInCatalog = true,
        Guid? IntegrationId = null);
}
