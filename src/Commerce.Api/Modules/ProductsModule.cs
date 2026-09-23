using System.Text.Json.Serialization;
using System.Text.Json;
using Carter;
using Finbuckle.MultiTenant.Abstractions;
using MediatR;
using Microsoft.AspNetCore.Http.Features;
using Commerce.Application.Features.CreateProduct;
using Commerce.Application.Features.CreateProduct.ApproveOnboarding;
using Commerce.Application.Features.CreateProduct.StartOnboarding;
using Commerce.Domain.Tenants;

namespace Commerce.Api.Modules;

public sealed class ProductsModule : ICarterModule
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    public void AddRoutes(IEndpointRouteBuilder app)
    {
        Map(app.MapGroup("/t/{__tenant__}/products").WithTags("Products"));
    }

    private static void Map(RouteGroupBuilder group)
    {
        group.MapPost("/onboarding/stream", StreamOnboardingAsync).DisableAntiforgery();
        group.MapPost("/onboarding/{workflowId}/approve", ApproveAsync);
        group.MapGet("/onboarding/{workflowId}/image", GetImageAsync);
    }

    private static async Task StreamOnboardingAsync(
        HttpContext http,
        IMediator mediator,
        IMultiTenantContextAccessor<CommerceTenantInfo> tenants,
        CancellationToken cancellationToken)
    {
        var tenant = tenants.MultiTenantContext?.TenantInfo;
        if (tenant?.Id is null)
        {
            http.Response.StatusCode = StatusCodes.Status400BadRequest;
            await http.Response.WriteAsJsonAsync(new { error = "Tenant is required." }, cancellationToken);
            return;
        }

        var form = await http.Request.ReadFormAsync(cancellationToken);
        var title = form["title"].ToString();
        if (string.IsNullOrWhiteSpace(title))
            title = form["name"].ToString();

        Guid? integrationId = null;
        if (Guid.TryParse(form["integrationId"].ToString(), out var parsedIntegration))
            integrationId = parsedIntegration;

        var file = form.Files.GetFile("image") ?? form.Files.GetFile("photo");
        Stream? imageStream = null;
        string? contentType = null;
        if (file is { Length: > 0 })
        {
            imageStream = file.OpenReadStream();
            contentType = file.ContentType;
        }

        var publicBase = $"{http.Request.Scheme}://{http.Request.Host}";

        http.Features.Get<IHttpResponseBodyFeature>()?.DisableBuffering();
        http.Response.StatusCode = StatusCodes.Status200OK;
        http.Response.ContentType = "text/event-stream";
        http.Response.Headers.CacheControl = "no-cache";
        http.Response.Headers.Append("X-Accel-Buffering", "no");
        await http.Response.WriteAsync(": keepalive\n\n", cancellationToken);
        await http.Response.Body.FlushAsync(cancellationToken);

        try
        {
            await WriteSseAsync(http, new { type = "status", text = "analyzing" }, cancellationToken);

            var result = await mediator.Send(
                new StartOnboardingCommand(
                    string.IsNullOrWhiteSpace(title) ? null : title,
                    ImageUrl: null,
                    imageStream,
                    contentType,
                    integrationId,
                    publicBase),
                cancellationToken);

            await WriteSseAsync(http, new
            {
                type = "approval_needed",
                workflow_id = result.WorkflowId,
                tenant_id = result.TenantId,
                draft = new
                {
                    brand = new
                    {
                        name = result.Draft.Brand.Name,
                        is_new = result.Draft.Brand.IsNew,
                        id = result.Draft.Brand.Id
                    },
                    category = new
                    {
                        name = result.Draft.Category.Name,
                        is_new = result.Draft.Category.IsNew,
                        id = result.Draft.Category.Id
                    },
                    product = new
                    {
                        title = result.Draft.Product.Title,
                        image_url = result.Draft.Product.ImageUrl
                    }
                }
            }, cancellationToken);

            await WriteSseAsync(http, new { type = "done" }, cancellationToken);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            await WriteSseAsync(http, new { type = "error", text = ex.Message }, cancellationToken);
        }
        finally
        {
            if (imageStream is not null)
                await imageStream.DisposeAsync();
        }
    }

    private static async Task<IResult> ApproveAsync(
        string workflowId,
        ApproveOnboardingRequest body,
        IMediator mediator,
        IMultiTenantContextAccessor<CommerceTenantInfo> tenants,
        CancellationToken cancellationToken)
    {
        if (tenants.MultiTenantContext?.TenantInfo?.Id is null)
            return Results.BadRequest(new { error = "Tenant is required." });

        if (string.IsNullOrWhiteSpace(workflowId))
            return Results.BadRequest(new { error = "workflow_id is required." });

        try
        {
            var result = await mediator.Send(
                new ApproveOnboardingCommand(
                    workflowId,
                    body.TenantId,
                    new OnboardingApprovals(
                        body.Approvals?.Brand ?? false,
                        body.Approvals?.Category ?? false,
                        body.Approvals?.Product ?? false)),
                cancellationToken);

            return Results.Ok(new
            {
                type = "created",
                workflow_id = result.WorkflowId,
                brand_id = result.BrandId,
                category_id = result.CategoryId,
                product_id = result.ProductId
            });
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

    private static async Task<IResult> GetImageAsync(
        string workflowId,
        IOnboardingImageStore images,
        IMultiTenantContextAccessor<CommerceTenantInfo> tenants,
        CancellationToken cancellationToken)
    {
        var tenant = tenants.MultiTenantContext?.TenantInfo;
        if (tenant?.Id is null)
            return Results.BadRequest(new { error = "Tenant is required." });

        var image = await images.GetAsync(tenant.Id, workflowId, cancellationToken);
        if (image is null)
            return Results.NotFound();

        return Results.File(image.Value.Bytes, image.Value.ContentType);
    }

    private static async Task WriteSseAsync(HttpContext http, object payload, CancellationToken cancellationToken)
    {
        var json = JsonSerializer.Serialize(payload, Json);
        await http.Response.WriteAsync($"data: {json}\n\n", cancellationToken);
        await http.Response.Body.FlushAsync(cancellationToken);
    }

    private sealed record ApproveOnboardingRequest(
        [property: JsonPropertyName("tenant_id")] string? TenantId = null,
        [property: JsonPropertyName("approvals")] ApproveFlags? Approvals = null);

    private sealed record ApproveFlags(
        [property: JsonPropertyName("brand")] bool Brand = false,
        [property: JsonPropertyName("category")] bool Category = false,
        [property: JsonPropertyName("product")] bool Product = false);
}
