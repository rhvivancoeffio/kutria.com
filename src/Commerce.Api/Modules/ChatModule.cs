using System.Text.Json;
using Carter;
using Finbuckle.MultiTenant.Abstractions;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Commerce.Application.Abstracts;
using Commerce.Application.Features.CreateProduct;
using Commerce.Domain.Tenants;

namespace Commerce.Api.Modules;

public sealed class ChatModule : ICarterModule
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);
    private const long MaxImageBytes = 8 * 1024 * 1024;

    public void AddRoutes(IEndpointRouteBuilder app)
    {
        Map(app.MapGroup("/t/{__tenant__}/chat").WithTags("Chat"));
    }

    private static void Map(RouteGroupBuilder group)
    {
        group.MapPost("/stream", StreamAsync).RequireCors("Chat").DisableAntiforgery();
        group.MapPost("/runs", StartRunAsync).RequireCors("Chat").DisableAntiforgery();
        group.MapPost("/transcribe", TranscribeAsync).RequireCors("Chat").DisableAntiforgery();
        group.MapGet("/threads", ListThreadsAsync).RequireCors("Chat");
        group.MapGet("/threads/{threadId}", GetThreadAsync).RequireCors("Chat");
        group.MapGet("/attachments/{attachmentId}", GetAttachmentAsync).RequireCors("Chat");
    }

    private static async Task<IResult> ListThreadsAsync(
        string? audience,
        int? take,
        IChatThreadStore threads,
        IMultiTenantContextAccessor<CommerceTenantInfo> tenants,
        CancellationToken cancellationToken)
    {
        var tenant = tenants.MultiTenantContext?.TenantInfo;
        if (tenant?.Id is null)
            return Results.BadRequest(new { error = "Tenant is required." });

        var resolvedAudience = string.Equals(audience, "manager", StringComparison.OrdinalIgnoreCase)
            ? "manager"
            : "buyer";
        var items = await threads.ListAsync(
            tenant.Id,
            resolvedAudience,
            take ?? 50,
            cancellationToken);
        return Results.Ok(new
        {
            audience = resolvedAudience,
            threads = items.Select(t => new
            {
                threadId = t.ThreadId,
                title = t.Title,
                updatedAt = t.UpdatedAt,
                messageCount = t.MessageCount
            })
        });
    }

    private static async Task<IResult> GetThreadAsync(
        string threadId,
        string? audience,
        IChatThreadStore threads,
        IMultiTenantContextAccessor<CommerceTenantInfo> tenants,
        CancellationToken cancellationToken)
    {
        var tenant = tenants.MultiTenantContext?.TenantInfo;
        if (tenant?.Id is null)
            return Results.BadRequest(new { error = "Tenant is required." });

        if (string.IsNullOrWhiteSpace(threadId))
            return Results.BadRequest(new { error = "threadId is required." });

        var resolvedAudience = string.Equals(audience, "manager", StringComparison.OrdinalIgnoreCase)
            ? "manager"
            : "buyer";
        var messages = await threads.GetAsync(
            tenant.Id,
            resolvedAudience,
            threadId.Trim(),
            cancellationToken);
        return Results.Ok(new
        {
            threadId = threadId.Trim(),
            audience = resolvedAudience,
            messages = messages.Select(m => new { role = m.Role, text = m.Text })
        });
    }

    private static async Task<IResult> TranscribeAsync(
        IFormFile? audio,
        ISpeechTranscriber speech,
        IMultiTenantContextAccessor<CommerceTenantInfo> tenants,
        CancellationToken cancellationToken)
    {
        if (tenants.MultiTenantContext?.TenantInfo?.Id is null)
        {
            return Results.BadRequest(new { error = "Tenant is required." });
        }

        if (audio is null || audio.Length == 0)
        {
            return Results.BadRequest(new { error = "El audio es requerido." });
        }

        if (audio.Length > 10 * 1024 * 1024)
        {
            return Results.BadRequest(new { error = "El audio debe pesar máximo 10MB." });
        }

        try
        {
            await using var stream = audio.OpenReadStream();
            var text = await speech.TranscribeAsync(stream, audio.ContentType, cancellationToken);
            return Results.Ok(new { text });
        }
        catch (InvalidOperationException ex)
        {
            return Results.BadRequest(new { error = ex.Message });
        }
    }

    private static async Task<IResult> StartRunAsync(
        HttpContext http,
        IOnboardingImageStore images,
        IMultiTenantContextAccessor<CommerceTenantInfo> tenants,
        IServiceScopeFactory scopes,
        ILogger<ChatModule> logger,
        CancellationToken cancellationToken)
    {
        var tenant = tenants.MultiTenantContext?.TenantInfo;
        if (tenant?.Id is null)
            return Results.BadRequest(new { error = "Tenant is required." });

        var prepared = await TryReadChatInputAsync(http, images, tenant, cancellationToken);
        if (prepared.Error is not null)
            return Results.BadRequest(new { error = prepared.Error });

        var (prompt, threadId, audience, image) = prepared.Value!.Value;
        var streamId = Guid.NewGuid().ToString("N");
        var tenantId = tenant.Id;
        var tenantInfo = tenant;

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
                    threadId,
                    audience,
                    image,
                    streamId,
                    cancellationToken: CancellationToken.None))
                {
                    // Progress is persisted by AgentRuntime into IEventStreamStore.
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Chat background run failed. StreamId={StreamId} ThreadId={ThreadId}", streamId, threadId);
                try
                {
                    await using var scope = scopes.CreateAsyncScope();
                    var store = scope.ServiceProvider.GetRequiredService<IEventStreamStore>();
                    await store.AppendAsync(tenantId, streamId, "error", ex.Message);
                }
                catch (Exception appendEx)
                {
                    logger.LogWarning(appendEx, "Failed to append error event for StreamId={StreamId}", streamId);
                }
            }
        }, CancellationToken.None);

        return Results.Ok(new { streamId, threadId, audience });
    }

    private static async Task StreamAsync(
        HttpContext http,
        IAgentRuntime runtime,
        IOnboardingImageStore images,
        IMultiTenantContextAccessor<CommerceTenantInfo> tenants,
        ILogger<ChatModule> logger,
        CancellationToken cancellationToken)
    {
        var tenant = tenants.MultiTenantContext?.TenantInfo;
        if (tenant?.Id is null)
        {
            http.Response.StatusCode = StatusCodes.Status400BadRequest;
            await http.Response.WriteAsJsonAsync(new { error = "Tenant is required." }, cancellationToken);
            return;
        }

        var prepared = await TryReadChatInputAsync(http, images, tenant, cancellationToken);
        if (prepared.Error is not null)
        {
            http.Response.StatusCode = StatusCodes.Status400BadRequest;
            await http.Response.WriteAsJsonAsync(new { error = prepared.Error }, cancellationToken);
            return;
        }

        var (prompt, threadId, audience, image) = prepared.Value!.Value;
        var streamId = Guid.NewGuid().ToString("N");

        http.Features.Get<IHttpResponseBodyFeature>()?.DisableBuffering();
        http.Response.StatusCode = StatusCodes.Status200OK;
        http.Response.ContentType = "text/event-stream";
        http.Response.Headers.CacheControl = "no-cache";
        http.Response.Headers.Append("X-Accel-Buffering", "no");
        await http.Response.WriteAsync(": keepalive\n\n", cancellationToken);
        await http.Response.Body.FlushAsync(cancellationToken);

        using var abortWatch = http.RequestAborted.Register(() =>
        {
            logger.LogWarning(
                "Chat SSE RequestAborted. ThreadId={ThreadId} StreamId={StreamId} Audience={Audience} ConnectionId={ConnectionId} Remote={Remote} ResponseStarted={ResponseStarted}",
                threadId,
                streamId,
                audience,
                http.Connection.Id,
                http.Connection.RemoteIpAddress,
                http.Response.HasStarted);
        });

        try
        {
            await foreach (var evt in runtime.StreamAsync(
                prompt,
                tenant.Id,
                threadId,
                audience,
                image,
                streamId,
                cancellationToken: http.RequestAborted))
            {
                var payload = JsonSerializer.Serialize(new
                {
                    type = evt.Type,
                    text = evt.Text,
                    toolName = evt.ToolName,
                    agentKey = evt.AgentKey,
                    threadId = evt.ThreadId ?? threadId,
                    streamId
                }, Json);
                await http.Response.WriteAsync($"event: {evt.Type}\ndata: {payload}\n\n", http.RequestAborted);
                await http.Response.Body.FlushAsync(http.RequestAborted);
            }
        }
        catch (OperationCanceledException) when (http.RequestAborted.IsCancellationRequested)
        {
            logger.LogWarning(
                "Chat SSE ended by RequestAborted. ThreadId={ThreadId} StreamId={StreamId} Audience={Audience} ConnectionId={ConnectionId}",
                threadId,
                streamId,
                audience,
                http.Connection.Id);
        }
    }

    private static async Task<(string? Error, (string Prompt, string ThreadId, string Audience, ChatImageAttachment? Image)? Value)> TryReadChatInputAsync(
        HttpContext http,
        IOnboardingImageStore images,
        CommerceTenantInfo tenant,
        CancellationToken cancellationToken)
    {
        string? message;
        string? threadIdRaw;
        string? audienceRaw;
        IFormFile? imageFile = null;

        if (http.Request.HasFormContentType)
        {
            var form = await http.Request.ReadFormAsync(cancellationToken);
            message = form["message"].ToString();
            threadIdRaw = form["threadId"].ToString();
            audienceRaw = form["audience"].ToString();
            imageFile = form.Files.GetFile("image")
                ?? form.Files.GetFile("photo")
                ?? form.Files.FirstOrDefault(f => f.ContentType.StartsWith("image/", StringComparison.OrdinalIgnoreCase));
        }
        else
        {
            var body = await http.Request.ReadFromJsonAsync<ChatRequest>(Json, cancellationToken);
            message = body?.Message;
            threadIdRaw = body?.ThreadId;
            audienceRaw = body?.Audience;
        }

        ChatImageAttachment? image = null;
        if (imageFile is not null && imageFile.Length > 0)
        {
            if (imageFile.Length > MaxImageBytes)
                return ("La imagen debe pesar máximo 8MB.", null);

            if (!string.IsNullOrWhiteSpace(imageFile.ContentType)
                && !imageFile.ContentType.StartsWith("image/", StringComparison.OrdinalIgnoreCase))
                return ("El archivo debe ser una imagen.", null);

            await using var copy = new MemoryStream();
            await imageFile.CopyToAsync(copy, cancellationToken);
            var bytes = copy.ToArray();
            if (bytes.Length > 0)
            {
                var attachmentId = $"chat_{Guid.NewGuid():N}";
                var contentType = string.IsNullOrWhiteSpace(imageFile.ContentType) ? "image/jpeg" : imageFile.ContentType;
                await images.SaveAsync(tenant.Id!, attachmentId, bytes, contentType, cancellationToken);
                var relative = $"/t/{tenant.Identifier}/chat/attachments/{attachmentId}";
                var baseUrl = $"{http.Request.Scheme}://{http.Request.Host.Value}";
                image = new ChatImageAttachment(attachmentId, bytes, contentType, $"{baseUrl}{relative}");
            }
        }

        if (string.IsNullOrWhiteSpace(message) && image is null)
            return ("Message or image is required.", null);

        var threadId = string.IsNullOrWhiteSpace(threadIdRaw) ? Guid.NewGuid().ToString("N") : threadIdRaw.Trim();
        var audience = string.Equals(audienceRaw, "manager", StringComparison.OrdinalIgnoreCase) ? "manager" : "buyer";
        var prompt = message?.Trim() ?? string.Empty;
        return (null, (prompt, threadId, audience, image));
    }

    private static async Task<IResult> GetAttachmentAsync(
        string attachmentId,
        IOnboardingImageStore images,
        IMultiTenantContextAccessor<CommerceTenantInfo> tenants,
        CancellationToken cancellationToken)
    {
        var tenant = tenants.MultiTenantContext?.TenantInfo;
        if (tenant?.Id is null)
            return Results.BadRequest(new { error = "Tenant is required." });

        if (string.IsNullOrWhiteSpace(attachmentId))
            return Results.BadRequest(new { error = "attachmentId is required." });

        var image = await images.GetAsync(tenant.Id, attachmentId, cancellationToken);
        if (image is null)
            return Results.NotFound();

        return Results.File(image.Value.Bytes, image.Value.ContentType);
    }

    private sealed record ChatRequest(string? Message, string? ThreadId, string? Audience);
}
