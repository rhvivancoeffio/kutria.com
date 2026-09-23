using Carter;
using FluentValidation;
using MediatR;
using Commerce.Application.Features.Brains.GetBrainJob;
using Commerce.Application.Features.Brains.PublishBrainDocument;
using Commerce.Application.Features.Brains.UploadBrainDocument;

namespace Commerce.Api.Modules;

public sealed class BrainModule : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        Map(app.MapGroup("/t/{__tenant__}/brains").WithTags("Brains"));
    }

    private static void Map(RouteGroupBuilder group)
    {
        group.MapPost("/{brainKey}", Upload);
        group.MapGet("/jobs/{id:guid}", async (Guid id, IMediator mediator, CancellationToken cancellationToken) =>
        {
            var result = await mediator.Send(new GetBrainJobQuery(id), cancellationToken);
            return result is null ? Results.NotFound() : Results.Ok(result);
        });
        group.MapPost("/jobs/{id:guid}/publish", async (Guid id, PublishBody? body, IMediator mediator, CancellationToken cancellationToken) =>
        {
            try
            {
                var result = await mediator.Send(new PublishBrainDocumentCommand(id, body?.Text), cancellationToken);
                return Results.Json(result, statusCode: StatusCodes.Status202Accepted);
            }
            catch (InvalidOperationException ex)
            {
                return Results.BadRequest(new { error = ex.Message });
            }
        });
    }

    private static async Task<IResult> Upload(
        string brainKey,
        HttpRequest request,
        IMediator mediator,
        CancellationToken cancellationToken)
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
                new UploadBrainDocumentCommand(
                    brainKey,
                    file.FileName,
                    file.ContentType,
                    buffer.ToArray(),
                    form["metadata"].ToString()),
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
