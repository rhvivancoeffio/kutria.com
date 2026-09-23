using System.Text.Json;
using Carter;
using FluentValidation;
using MediatR;
using Microsoft.AspNetCore.Http.Features;
using Commerce.Application.Abstracts;
using Commerce.Application.Features.Policies.DownloadPolicyFile;
using Commerce.Application.Features.Policies.GetPolicyJob;
using Commerce.Application.Features.Policies.ListPublishedPolicies;
using Commerce.Application.Features.Policies.PublishPolicy;
using Commerce.Application.Features.Policies.GetPolicyEvalStatus;
using Commerce.Domain.Brains;
using Commerce.Application.Features.Policies.GetPolicyEvalItems;
using Commerce.Application.Features.Policies.ListPolicyJobEvals;
using Commerce.Application.Features.Policies.SavePolicyEvalItems;
using Commerce.Application.Features.Policies.StartPolicyEval;
using Commerce.Application.Features.Policies.UploadPolicy;

namespace Commerce.Api.Modules;

public sealed class PoliciesModule : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        Map(app.MapGroup("/t/{__tenant__}/policies").WithTags("Policies"));
    }

    private static void Map(RouteGroupBuilder group)
    {
        group.MapGet("/", async (IMediator mediator, CancellationToken cancellationToken) =>
            Results.Ok(await mediator.Send(new ListPublishedPoliciesQuery(), cancellationToken)));
        group.MapPost("/", Upload);
        group.MapGet("/jobs/{id:guid}/file", async (Guid id, IMediator mediator, CancellationToken cancellationToken) =>
        {
            var file = await mediator.Send(new DownloadPolicyFileQuery(id), cancellationToken);
            return file is null
                ? Results.NotFound()
                : Results.File(file.Content, file.ContentType, file.FileName);
        });
        group.MapGet("/eval-items", async (IMediator mediator, CancellationToken cancellationToken) =>
            Results.Ok(await mediator.Send(new GetPolicyEvalItemsQuery(), cancellationToken)));
        group.MapPut("/eval-items", async (SavePolicyEvalItemsCommand command, IMediator mediator, CancellationToken cancellationToken) =>
        {
            try
            {
                return Results.Ok(await mediator.Send(command, cancellationToken));
            }
            catch (ValidationException ex)
            {
                return Results.BadRequest(new { error = ex.Errors.FirstOrDefault()?.ErrorMessage ?? ex.Message });
            }
        });
        group.MapPost("/jobs/{id:guid}/eval", async (Guid id, IMediator mediator, CancellationToken cancellationToken) =>
        {
            try
            {
                return Results.Accepted($"/policies/jobs/{id}/evals", await mediator.Send(new StartPolicyEvalCommand(id), cancellationToken));
            }
            catch (InvalidOperationException ex)
            {
                return Results.BadRequest(new { error = ex.Message });
            }
        });
        group.MapGet("/jobs/{id:guid}/evals", async (Guid id, IMediator mediator, CancellationToken cancellationToken) =>
            Results.Ok(await mediator.Send(new ListPolicyJobEvalsQuery(id), cancellationToken)));
        group.MapGet("/jobs/{id:guid}/eval/events", WatchEvalAsync);
        group.MapGet("/jobs/{id:guid}", async (Guid id, IMediator mediator, CancellationToken cancellationToken) =>
        {
            var result = await mediator.Send(new GetPolicyJobQuery(id), cancellationToken);
            return result is null ? Results.NotFound() : Results.Ok(result);
        });
        group.MapGet("/jobs/{id:guid}/events", WatchAsync);
        group.MapPost("/jobs/{id:guid}/publish", async (Guid id, PublishBody? body, IMediator mediator, CancellationToken cancellationToken) =>
        {
            try
            {
                var result = await mediator.Send(new PublishPolicyCommand(id, body?.Text), cancellationToken);
                return Results.Json(result, statusCode: StatusCodes.Status202Accepted);
            }
            catch (InvalidOperationException ex)
            {
                return Results.BadRequest(new { error = ex.Message });
            }
        });
    }

    private static async Task WatchEvalAsync(
        Guid id,
        HttpContext http,
        IMediator mediator,
        IEventStreamStore eventStreams,
        Finbuckle.MultiTenant.Abstractions.IMultiTenantContextAccessor<Commerce.Domain.Tenants.CommerceTenantInfo> tenants,
        CancellationToken cancellationToken)
    {
        var first = await mediator.Send(new GetPolicyEvalStatusQuery(id), cancellationToken);
        if (first is null)
        {
            http.Response.StatusCode = StatusCodes.Status404NotFound;
            return;
        }

        var tenantId = tenants.MultiTenantContext?.TenantInfo?.Id;
        if (string.IsNullOrWhiteSpace(tenantId))
        {
            http.Response.StatusCode = StatusCodes.Status400BadRequest;
            return;
        }

        http.Features.Get<IHttpResponseBodyFeature>()?.DisableBuffering();
        http.Response.StatusCode = StatusCodes.Status200OK;
        http.Response.ContentType = "text/event-stream";
        http.Response.Headers.CacheControl = "no-cache";
        http.Response.Headers.Append("X-Accel-Buffering", "no");

        var streamId = EventStreamKeys.ForPolicyEval(id);
        string? after = null;
        var deadline = DateTimeOffset.UtcNow.AddMinutes(15);
        while (!cancellationToken.IsCancellationRequested && DateTimeOffset.UtcNow < deadline)
        {
            var batch = await eventStreams.ListSinceAsync(tenantId, streamId, after, 50, cancellationToken);
            foreach (var evt in batch.Items)
            {
                var payload = MapEvalStreamEvent(evt, id);
                await http.Response.WriteAsync($"data: {payload}\n\n", cancellationToken);
                await http.Response.Body.FlushAsync(cancellationToken);
                after = evt.RowKey;
                if (EventStreamKeys.IsTerminal(evt.Type))
                    return;
            }

            if (batch.Completed)
                return;

            // Fallback while worker has not appended yet: mirror DB status once.
            if (batch.Items.Count == 0 && after is null)
            {
                var status = await mediator.Send(new GetPolicyEvalStatusQuery(id), cancellationToken);
                if (status is not null)
                {
                    var payload = JsonSerializer.Serialize(new
                    {
                        status.Id,
                        status.EvaluationStatus,
                        status.FailedCount,
                        status.Answered,
                        message = DescribeEval(status)
                    }, Json);
                    await http.Response.WriteAsync($"data: {payload}\n\n", cancellationToken);
                    await http.Response.Body.FlushAsync(cancellationToken);
                    if (status.EvaluationStatus is PolicyEvaluationStatus.Passed or PolicyEvaluationStatus.ReviewFailed)
                        return;
                }
            }

            await Task.Delay(TimeSpan.FromMilliseconds(800), cancellationToken);
        }
    }

    private static string MapEvalStreamEvent(StreamEvent evt, Guid jobId)
    {
        if (!string.IsNullOrWhiteSpace(evt.Data))
        {
            try
            {
                using var doc = JsonDocument.Parse(evt.Data);
                var root = doc.RootElement;
                return JsonSerializer.Serialize(new
                {
                    id = root.TryGetProperty("id", out var idEl) && idEl.TryGetGuid(out var gid) ? gid : jobId,
                    evaluationStatus = root.TryGetProperty("evaluationStatus", out var st) ? st.GetString() : null,
                    failedCount = root.TryGetProperty("failedCount", out var fc) && fc.TryGetInt32(out var f) ? f : 0,
                    answered = root.TryGetProperty("answered", out var an) && an.TryGetInt32(out var a) ? a : 0,
                    message = root.TryGetProperty("message", out var msg) ? msg.GetString() : evt.Text,
                    type = evt.Type
                }, Json);
            }
            catch (JsonException)
            {
                // fall through
            }
        }

        return JsonSerializer.Serialize(new
        {
            id = jobId,
            evaluationStatus = EventStreamKeys.IsTerminal(evt.Type) ? evt.Type : "pending",
            failedCount = 0,
            answered = 0,
            message = evt.Text,
            type = evt.Type
        }, Json);
    }

    private static string DescribeEval(GetPolicyEvalStatusResult status)
        => status.EvaluationStatus switch
        {
            PolicyEvaluationStatus.Pending => status.Answered == 0
                ? "En cola."
                : $"En curso: {status.Answered} preguntas, {status.FailedCount} fallos.",
            PolicyEvaluationStatus.Passed => $"Aprobado. {status.FailedCount} fallos.",
            PolicyEvaluationStatus.ReviewFailed => $"Review Failed. {status.FailedCount} fallos.",
            _ => "Sin evaluar."
        };

    private static async Task WatchAsync(
        Guid id,
        HttpContext http,
        IMediator mediator,
        string? phase,
        CancellationToken cancellationToken)
    {
        var first = await mediator.Send(new GetPolicyJobQuery(id), cancellationToken);
        if (first is null)
        {
            http.Response.StatusCode = StatusCodes.Status404NotFound;
            return;
        }

        http.Features.Get<IHttpResponseBodyFeature>()?.DisableBuffering();
        http.Response.StatusCode = StatusCodes.Status200OK;
        http.Response.ContentType = "text/event-stream";
        http.Response.Headers.CacheControl = "no-cache";
        http.Response.Headers.Append("X-Accel-Buffering", "no");

        var seen = string.Empty;
        var deadline = DateTimeOffset.UtcNow.AddMinutes(10);
        while (!cancellationToken.IsCancellationRequested && DateTimeOffset.UtcNow < deadline)
        {
            var job = await mediator.Send(new GetPolicyJobQuery(id), cancellationToken);
            if (job is null)
            {
                break;
            }

            var signature = $"{job.Status}|{job.PageCount}|{job.TableCount}|{job.Summary}|{job.Error}";
            if (!string.Equals(signature, seen, StringComparison.Ordinal))
            {
                seen = signature;
                var payload = JsonSerializer.Serialize(new
                {
                    job.Id,
                    job.Status,
                    job.FileName,
                    job.PageCount,
                    job.TableCount,
                    job.Text,
                    job.Summary,
                    job.Error,
                    message = Describe(job)
                }, Json);
                await http.Response.WriteAsync($"data: {payload}\n\n", cancellationToken);
                await http.Response.Body.FlushAsync(cancellationToken);
            }

            if (IsTerminal(job.Status, phase))
            {
                break;
            }

            await Task.Delay(TimeSpan.FromSeconds(1), cancellationToken);
        }
    }

    private static bool IsTerminal(string status, string? phase)
    {
        if (string.Equals(phase, "publish", StringComparison.OrdinalIgnoreCase))
        {
            return status is "published" or "failed";
        }

        return status is "ready" or "needs_ocr" or "published" or "failed";
    }

    private static string Describe(GetPolicyJobResult job)
        => job.Status switch
        {
            "queued" => "Archivo recibido. En cola.",
            "reading" => "Leyendo el archivo.",
            "ready" => $"Archivo leído: {job.PageCount ?? 0} páginas, {job.TableCount ?? 0} tablas.",
            "needs_ocr" => "Necesita OCR. El original se guardó y no se publica.",
            "publishing" => "Publicando en el cerebro.",
            "published" => "Publicado.",
            "failed" => job.Error ?? "No se pudo leer el archivo.",
            _ => job.Status
        };

    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    private static async Task<IResult> Upload(HttpRequest request, IMediator mediator, CancellationToken cancellationToken)
    {
        if (!request.HasFormContentType)
        {
            return Results.BadRequest(new { error = "A file is required." });
        }

        var form = await request.ReadFormAsync(cancellationToken);
        var file = form.Files.GetFile("file");
        if (file is null || file.Length == 0)
        {
            return Results.BadRequest(new { error = "A file is required." });
        }

        await using var stream = file.OpenReadStream();
        using var buffer = new MemoryStream();
        await stream.CopyToAsync(buffer, cancellationToken);
        try
        {
            var result = await mediator.Send(
                new UploadPolicyCommand(
                    file.FileName,
                    file.ContentType,
                    buffer.ToArray(),
                    form["type"].ToString(),
                    form["effectiveFrom"].ToString()),
                cancellationToken);
            return Results.Json(result, statusCode: StatusCodes.Status202Accepted);
        }
        catch (ValidationException ex)
        {
            return Results.BadRequest(new { error = ex.Errors.FirstOrDefault()?.ErrorMessage ?? ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return Results.BadRequest(new { error = ex.Message });
        }
    }

    private sealed record PublishBody(string? Text);
}
