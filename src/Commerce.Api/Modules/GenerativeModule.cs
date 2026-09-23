using System.Text.Json;
using Carter;
using Finbuckle.MultiTenant.Abstractions;
using FluentValidation;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Commerce.Application.Abstracts;
using Commerce.Application.Features.CreateProduct;
using Commerce.Application.Features.Generative.StartProductContentByImageGeneration;
using Commerce.Application.Features.Generative.StartProductContentGeneration;
using Commerce.Application.Features.Generative.StartProductImageGeneration;
using Commerce.Application.Features.Generative.StartProductVideoGeneration;
using Commerce.Domain.Tenants;

namespace Commerce.Api.Modules;

public sealed class GenerativeModule : ICarterModule
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    public void AddRoutes(IEndpointRouteBuilder app)
    {
        Map(app.MapGroup("/t/{__tenant__}/generative").WithTags("Generative"));
    }

    private static void Map(RouteGroupBuilder group)
    {
        group.MapPost("/content-by-image/runs", StartContentByImageAsync).DisableAntiforgery();
        group.MapPost("/content/runs", StartContentAsync);
        group.MapPost("/images/runs", StartImagesAsync);
        group.MapPost("/video/runs", StartVideoAsync);
    }

    private const long MaxImageBytes = 8 * 1024 * 1024;

    private static async Task<IResult> StartContentByImageAsync(
        HttpContext http,
        IOnboardingImageStore images,
        IMediator mediator,
        IMultiTenantContextAccessor<CommerceTenantInfo> tenants,
        IServiceScopeFactory scopes,
        ILogger<GenerativeModule> logger,
        CancellationToken cancellationToken)
    {
        var tenant = tenants.MultiTenantContext?.TenantInfo;
        if (tenant?.Id is null)
            return Results.BadRequest(new { error = "Tenant is required." });

        string? imageUrl = null;
        string? imageAttachmentId = null;
        string? hint = null;
        string? brandId = null;
        string? brandName = null;
        string? categoryId = null;
        string? categoryPath = null;

        if (http.Request.HasFormContentType)
        {
            var form = await http.Request.ReadFormAsync(cancellationToken);
            hint = NullIfWhiteSpace(form["hint"].ToString());
            imageUrl = NullIfWhiteSpace(form["imageUrl"].ToString());
            imageAttachmentId = NullIfWhiteSpace(form["imageAttachmentId"].ToString());
            brandId = NullIfWhiteSpace(form["brandId"].ToString());
            brandName = NullIfWhiteSpace(form["brandName"].ToString());
            categoryId = NullIfWhiteSpace(form["categoryId"].ToString());
            categoryPath = NullIfWhiteSpace(form["categoryPath"].ToString());

            var imageFile = form.Files.GetFile("image")
                ?? form.Files.GetFile("photo")
                ?? form.Files.FirstOrDefault(f =>
                    f.ContentType.StartsWith("image/", StringComparison.OrdinalIgnoreCase));

            if (imageFile is { Length: > 0 })
            {
                if (imageFile.Length > MaxImageBytes)
                    return Results.BadRequest(new { error = "La imagen debe pesar máximo 8MB." });
                if (!string.IsNullOrWhiteSpace(imageFile.ContentType)
                    && !imageFile.ContentType.StartsWith("image/", StringComparison.OrdinalIgnoreCase))
                    return Results.BadRequest(new { error = "El archivo debe ser una imagen." });

                await using var copy = new MemoryStream();
                await imageFile.CopyToAsync(copy, cancellationToken);
                var bytes = copy.ToArray();
                if (bytes.Length > 0)
                {
                    imageAttachmentId = $"gen_{Guid.NewGuid():N}";
                    var contentType = string.IsNullOrWhiteSpace(imageFile.ContentType)
                        ? "image/jpeg"
                        : imageFile.ContentType;
                    await images.SaveAsync(tenant.Id!, imageAttachmentId, bytes, contentType, cancellationToken);
                    var relative = $"/t/{tenant.Identifier}/chat/attachments/{imageAttachmentId}";
                    imageUrl = $"{http.Request.Scheme}://{http.Request.Host.Value}{relative}";
                }
            }
        }
        else
        {
            var body = await http.Request.ReadFromJsonAsync<StartContentByImageBody>(Json, cancellationToken);
            imageUrl = NullIfWhiteSpace(body?.ImageUrl);
            imageAttachmentId = NullIfWhiteSpace(body?.ImageAttachmentId);
            hint = NullIfWhiteSpace(body?.Hint);
            brandId = NullIfWhiteSpace(body?.BrandId);
            brandName = NullIfWhiteSpace(body?.BrandName);
            categoryId = NullIfWhiteSpace(body?.CategoryId);
            categoryPath = NullIfWhiteSpace(body?.CategoryPath);
        }

        StartProductContentByImageGenerationResult started;
        try
        {
            started = await mediator.Send(
                new StartProductContentByImageGenerationCommand(
                    imageUrl,
                    imageAttachmentId,
                    hint,
                    brandId,
                    brandName,
                    categoryId,
                    categoryPath),
                cancellationToken);
        }
        catch (ValidationException ex)
        {
            return Results.BadRequest(new { error = ex.Errors.FirstOrDefault()?.ErrorMessage ?? "Invalid data." });
        }
        catch (InvalidOperationException ex)
        {
            return Results.BadRequest(new { error = ex.Message });
        }

        var tenantInfo = tenant;
        var tenantId = tenant.Id;
        imageUrl = started.ImageUrl ?? imageUrl;
        imageAttachmentId = started.ImageAttachmentId ?? imageAttachmentId;
        var prompt = started.Prompt;
        var processId = started.ProcessId;
        var agentKey = started.AgentKey;
        var kind = started.Kind;
        var summary = JsonSerializer.Serialize(new
        {
            kind,
            hasImage = true,
            imageUrl,
            imageAttachmentId,
            hint,
            brandId,
            brandName,
            categoryId,
            categoryPath
        }, Json);

        _ = Task.Run(async () =>
        {
            try
            {
                await using var scope = scopes.CreateAsyncScope();
                var setter = scope.ServiceProvider.GetRequiredService<IMultiTenantContextSetter>();
                setter.MultiTenantContext = new MultiTenantContext<CommerceTenantInfo>(tenantInfo);

                ChatImageAttachment? image = null;
                if (!string.IsNullOrWhiteSpace(imageAttachmentId))
                {
                    var store = scope.ServiceProvider.GetRequiredService<IOnboardingImageStore>();
                    var stored = await store.GetAsync(tenantId, imageAttachmentId, CancellationToken.None);
                    if (stored is not null)
                    {
                        image = new ChatImageAttachment(
                            imageAttachmentId,
                            stored.Value.Bytes,
                            stored.Value.ContentType,
                            imageUrl ?? $"/t/{tenantInfo.Identifier}/chat/attachments/{imageAttachmentId}");
                    }
                }

                if (image is null && !string.IsNullOrWhiteSpace(imageUrl))
                {
                    image = new ChatImageAttachment(
                        imageAttachmentId ?? Guid.NewGuid().ToString("N"),
                        [],
                        "image/jpeg",
                        imageUrl);
                }

                var runtime = scope.ServiceProvider.GetRequiredService<IAgentRuntime>();
                await foreach (var _ in runtime.StreamAsync(
                    prompt,
                    tenantId,
                    threadId: processId,
                    audience: "manager",
                    image: image,
                    streamId: processId,
                    agentKey: agentKey,
                    generationKind: kind,
                    inputSummaryJson: summary,
                    cancellationToken: CancellationToken.None))
                {
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Generative content-by-image failed. ProcessId={ProcessId}", processId);
                try
                {
                    await using var scope = scopes.CreateAsyncScope();
                    var store = scope.ServiceProvider.GetRequiredService<IEventStreamStore>();
                    await store.AppendAsync(tenantId, processId, "error", ex.Message);
                }
                catch (Exception appendEx)
                {
                    logger.LogWarning(appendEx, "Failed to append error for ProcessId={ProcessId}", processId);
                }
            }
        }, CancellationToken.None);

        return Results.Json(new { processId, agentKey, imageUrl, imageAttachmentId }, statusCode: StatusCodes.Status202Accepted);
    }

    private static string? NullIfWhiteSpace(string? value)
        => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static async Task<IResult> StartContentAsync(
        StartContentBody? body,
        IMediator mediator,
        IMultiTenantContextAccessor<CommerceTenantInfo> tenants,
        IServiceScopeFactory scopes,
        ILogger<GenerativeModule> logger,
        CancellationToken cancellationToken)
    {
        var tenant = tenants.MultiTenantContext?.TenantInfo;
        if (tenant?.Id is null)
            return Results.BadRequest(new { error = "Tenant is required." });

        StartProductContentGenerationResult started;
        try
        {
            started = await mediator.Send(
                new StartProductContentGenerationCommand(
                    body?.Name ?? "",
                    body?.Modes,
                    body?.BrandId,
                    body?.BrandName,
                    body?.CategoryId,
                    body?.CategoryPath),
                cancellationToken);
        }
        catch (ValidationException ex)
        {
            return Results.BadRequest(new { error = ex.Errors.FirstOrDefault()?.ErrorMessage ?? "Invalid data." });
        }
        catch (InvalidOperationException ex)
        {
            return Results.BadRequest(new { error = ex.Message });
        }

        var tenantInfo = tenant;
        var tenantId = tenant.Id;
        var processId = started.ProcessId;
        var agentKey = started.AgentKey;
        var kind = started.Kind;
        var prompt = started.Prompt;
        var modes = body?.Modes is { Count: > 0 } ? body.Modes : new[] { "all" };
        var summary = JsonSerializer.Serialize(new
        {
            kind,
            name = body?.Name?.Trim(),
            modes,
            hasImage = false,
            brandId = NullIfWhiteSpace(body?.BrandId),
            brandName = NullIfWhiteSpace(body?.BrandName),
            categoryId = NullIfWhiteSpace(body?.CategoryId),
            categoryPath = NullIfWhiteSpace(body?.CategoryPath)
        }, Json);

        _ = Task.Run(async () =>
        {
            try
            {
                await using var scope = scopes.CreateAsyncScope();
                var setter = scope.ServiceProvider.GetRequiredService<IMultiTenantContextSetter>();
                setter.MultiTenantContext = new MultiTenantContext<CommerceTenantInfo>(tenantInfo);

                var runtime = scope.ServiceProvider.GetRequiredService<IAgentRuntime>();
                await foreach (var _ in runtime.StreamAsync(
                    prompt,
                    tenantId,
                    threadId: processId,
                    audience: "manager",
                    image: null,
                    streamId: processId,
                    agentKey: agentKey,
                    generationKind: kind,
                    inputSummaryJson: summary,
                    cancellationToken: CancellationToken.None))
                {
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Generative content failed. ProcessId={ProcessId}", processId);
                try
                {
                    await using var scope = scopes.CreateAsyncScope();
                    var store = scope.ServiceProvider.GetRequiredService<IEventStreamStore>();
                    await store.AppendAsync(tenantId, processId, "error", ex.Message);
                }
                catch (Exception appendEx)
                {
                    logger.LogWarning(appendEx, "Failed to append error for ProcessId={ProcessId}", processId);
                }
            }
        }, CancellationToken.None);

        return Results.Json(new { processId, agentKey }, statusCode: StatusCodes.Status202Accepted);
    }

    private static async Task<IResult> StartImagesAsync(
        StartImagesBody? body,
        IMediator mediator,
        IMultiTenantContextAccessor<CommerceTenantInfo> tenants,
        IServiceScopeFactory scopes,
        ILogger<GenerativeModule> logger,
        CancellationToken cancellationToken)
    {
        var tenant = tenants.MultiTenantContext?.TenantInfo;
        if (tenant?.Id is null)
            return Results.BadRequest(new { error = "Tenant is required." });

        StartProductImageGenerationResult started;
        try
        {
            started = await mediator.Send(
                new StartProductImageGenerationCommand(
                    body?.Name ?? "",
                    body?.Description,
                    body?.Count ?? 1),
                cancellationToken);
        }
        catch (ValidationException ex)
        {
            return Results.BadRequest(new { error = ex.Errors.FirstOrDefault()?.ErrorMessage ?? "Invalid data." });
        }
        catch (InvalidOperationException ex)
        {
            return Results.BadRequest(new { error = ex.Message });
        }

        var tenantInfo = tenant;
        var tenantId = tenant.Id;
        var processId = started.ProcessId;
        var agentKey = started.AgentKey;
        var kind = started.Kind;
        var prompt = started.Prompt;
        var summary = JsonSerializer.Serialize(new
        {
            kind,
            name = body?.Name?.Trim(),
            count = body?.Count ?? 1
        }, Json);

        _ = Task.Run(async () =>
        {
            try
            {
                await using var scope = scopes.CreateAsyncScope();
                var setter = scope.ServiceProvider.GetRequiredService<IMultiTenantContextSetter>();
                setter.MultiTenantContext = new MultiTenantContext<CommerceTenantInfo>(tenantInfo);

                var runtime = scope.ServiceProvider.GetRequiredService<IAgentRuntime>();
                await foreach (var _ in runtime.StreamAsync(
                    prompt,
                    tenantId,
                    threadId: processId,
                    audience: "manager",
                    image: null,
                    streamId: processId,
                    agentKey: agentKey,
                    generationKind: kind,
                    inputSummaryJson: summary,
                    cancellationToken: CancellationToken.None))
                {
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Generative images failed. ProcessId={ProcessId}", processId);
                try
                {
                    await using var scope = scopes.CreateAsyncScope();
                    var store = scope.ServiceProvider.GetRequiredService<IEventStreamStore>();
                    await store.AppendAsync(tenantId, processId, "error", ex.Message);
                }
                catch (Exception appendEx)
                {
                    logger.LogWarning(appendEx, "Failed to append error for ProcessId={ProcessId}", processId);
                }
            }
        }, CancellationToken.None);

        return Results.Json(new { processId, agentKey }, statusCode: StatusCodes.Status202Accepted);
    }

    private static async Task<IResult> StartVideoAsync(
        StartVideoBody? body,
        IMediator mediator,
        IMultiTenantContextAccessor<CommerceTenantInfo> tenants,
        IServiceScopeFactory scopes,
        ILogger<GenerativeModule> logger,
        CancellationToken cancellationToken)
    {
        var tenant = tenants.MultiTenantContext?.TenantInfo;
        if (tenant?.Id is null)
            return Results.BadRequest(new { error = "Tenant is required." });

        StartProductVideoGenerationResult started;
        try
        {
            started = await mediator.Send(
                new StartProductVideoGenerationCommand(
                    body?.Name ?? "",
                    body?.Description,
                    body?.ImageUrl),
                cancellationToken);
        }
        catch (ValidationException ex)
        {
            return Results.BadRequest(new { error = ex.Errors.FirstOrDefault()?.ErrorMessage ?? "Invalid data." });
        }
        catch (InvalidOperationException ex)
        {
            return Results.BadRequest(new { error = ex.Message });
        }

        var tenantInfo = tenant;
        var tenantId = tenant.Id;
        var processId = started.ProcessId;
        var agentKey = started.AgentKey;
        var kind = started.Kind;
        var prompt = started.Prompt;
        var summary = JsonSerializer.Serialize(new
        {
            kind,
            name = body?.Name?.Trim(),
            hasImage = !string.IsNullOrWhiteSpace(body?.ImageUrl)
        }, Json);

        _ = Task.Run(async () =>
        {
            try
            {
                await using var scope = scopes.CreateAsyncScope();
                var setter = scope.ServiceProvider.GetRequiredService<IMultiTenantContextSetter>();
                setter.MultiTenantContext = new MultiTenantContext<CommerceTenantInfo>(tenantInfo);

                var runtime = scope.ServiceProvider.GetRequiredService<IAgentRuntime>();
                await foreach (var _ in runtime.StreamAsync(
                    prompt,
                    tenantId,
                    threadId: processId,
                    audience: "manager",
                    image: null,
                    streamId: processId,
                    agentKey: agentKey,
                    generationKind: kind,
                    inputSummaryJson: summary,
                    cancellationToken: CancellationToken.None))
                {
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Generative video failed. ProcessId={ProcessId}", processId);
                try
                {
                    await using var scope = scopes.CreateAsyncScope();
                    var store = scope.ServiceProvider.GetRequiredService<IEventStreamStore>();
                    await store.AppendAsync(tenantId, processId, "error", ex.Message);
                }
                catch (Exception appendEx)
                {
                    logger.LogWarning(appendEx, "Failed to append error for ProcessId={ProcessId}", processId);
                }
            }
        }, CancellationToken.None);

        return Results.Json(new { processId, agentKey }, statusCode: StatusCodes.Status202Accepted);
    }

    private sealed record StartContentByImageBody(
        string? ImageUrl = null,
        string? ImageAttachmentId = null,
        string? Hint = null,
        string? BrandId = null,
        string? BrandName = null,
        string? CategoryId = null,
        string? CategoryPath = null);

    private sealed record StartContentBody(
        string? Name = null,
        IReadOnlyList<string>? Modes = null,
        string? BrandId = null,
        string? BrandName = null,
        string? CategoryId = null,
        string? CategoryPath = null);

    private sealed record StartVideoBody(
        string? Name = null,
        string? Description = null,
        string? ImageUrl = null);

    private sealed record StartImagesBody(
        string? Name = null,
        string? Description = null,
        int? Count = null);
}
