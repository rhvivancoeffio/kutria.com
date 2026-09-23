using Carter;
using MediatR;
using Commerce.Application.Features.DataIngestion.EnqueueDataIngest;
using Commerce.Application.Features.DataIngestion.ForceEnqueueCatalogVector;
using Commerce.Application.Features.DataIngestion.GetIngestedCatalogItem;
using Commerce.Application.Features.DataIngestion.GetIngestedOrder;
using Commerce.Application.Features.DataIngestion.GetIngestionCatalogItem;
using Commerce.Application.Features.DataIngestion.GetIngestionOrder;
using Commerce.Application.Features.DataIngestion.ListIngestedCatalog;
using Commerce.Application.Features.DataIngestion.ListIngestedOrders;
using Commerce.Application.Features.DataIngestion.ListIngestionsCatalog;
using Commerce.Application.Features.DataIngestion.ListIngestionsOrders;

namespace Commerce.Api.Modules;

public sealed class DataIngestionModule : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        Map(app.MapGroup("/t/{__tenant__}/integrations/{integrationId:guid}").WithTags("DataIngestion"));
    }

    private static void Map(RouteGroupBuilder group)
    {
        group.MapPost("/data-ingestion/enqueue", async (
            Guid integrationId,
            EnqueueRequest? body,
            IMediator mediator,
            CancellationToken ct) =>
        {
            try
            {
                var result = await mediator.Send(
                    new EnqueueDataIngestCommand(integrationId, body?.Kind),
                    ct);
                return Results.Accepted(value: result);
            }
            catch (KeyNotFoundException ex)
            {
                return Results.NotFound(new { error = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                return Results.BadRequest(new { error = ex.Message });
            }
        });

        group.MapGet("/catalog", async (
            Guid integrationId,
            int? page,
            int? pageSize,
            IMediator mediator,
            CancellationToken ct) =>
        {
            try
            {
                return Results.Ok(await mediator.Send(
                    new ListIngestedCatalogQuery(integrationId, page ?? 1, pageSize ?? 25),
                    ct));
            }
            catch (KeyNotFoundException ex)
            {
                return Results.NotFound(new { error = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                return Results.BadRequest(new { error = ex.Message });
            }
        });

        group.MapGet("/catalog/{id}", async (
            Guid integrationId,
            string id,
            IMediator mediator,
            CancellationToken ct) =>
        {
            try
            {
                var result = await mediator.Send(new GetIngestedCatalogItemQuery(integrationId, id), ct);
                return result is null ? Results.NotFound() : Results.Ok(result);
            }
            catch (KeyNotFoundException ex)
            {
                return Results.NotFound(new { error = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                return Results.BadRequest(new { error = ex.Message });
            }
        });

        group.MapGet("/orders", async (
            Guid integrationId,
            int? page,
            int? pageSize,
            IMediator mediator,
            CancellationToken ct) =>
        {
            try
            {
                return Results.Ok(await mediator.Send(
                    new ListIngestedOrdersQuery(integrationId, page ?? 1, pageSize ?? 25),
                    ct));
            }
            catch (KeyNotFoundException ex)
            {
                return Results.NotFound(new { error = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                return Results.BadRequest(new { error = ex.Message });
            }
        });

        group.MapGet("/orders/{id}", async (
            Guid integrationId,
            string id,
            IMediator mediator,
            CancellationToken ct) =>
        {
            try
            {
                var result = await mediator.Send(new GetIngestedOrderQuery(integrationId, id), ct);
                return result is null ? Results.NotFound() : Results.Ok(result);
            }
            catch (KeyNotFoundException ex)
            {
                return Results.NotFound(new { error = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                return Results.BadRequest(new { error = ex.Message });
            }
        });

        // Table Storage ingestions (nextToken pagination)
        group.MapGet("/ingestions/catalog", async (
            Guid integrationId,
            int? pageSize,
            string? nextToken,
            IMediator mediator,
            CancellationToken ct) =>
        {
            try
            {
                return Results.Ok(await mediator.Send(
                    new ListIngestionsCatalogQuery(integrationId, pageSize ?? 25, nextToken),
                    ct));
            }
            catch (KeyNotFoundException ex)
            {
                return Results.NotFound(new { error = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                return Results.BadRequest(new { error = ex.Message });
            }
        });

        group.MapGet("/ingestions/catalog/{id}", async (
            Guid integrationId,
            string id,
            IMediator mediator,
            CancellationToken ct) =>
        {
            try
            {
                var result = await mediator.Send(new GetIngestionCatalogItemQuery(integrationId, id), ct);
                return result is null ? Results.NotFound() : Results.Ok(result);
            }
            catch (KeyNotFoundException ex)
            {
                return Results.NotFound(new { error = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                return Results.BadRequest(new { error = ex.Message });
            }
        });

        group.MapPost("/ingestions/catalog/{id}/force-sync", async (
            Guid integrationId,
            string id,
            IMediator mediator,
            CancellationToken ct) =>
        {
            try
            {
                var result = await mediator.Send(new ForceEnqueueCatalogVectorCommand(integrationId, id), ct);
                return Results.Accepted(value: result);
            }
            catch (KeyNotFoundException ex)
            {
                return Results.NotFound(new { error = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                return Results.BadRequest(new { error = ex.Message });
            }
        });

        group.MapGet("/ingestions/orders", async (
            Guid integrationId,
            int? pageSize,
            string? nextToken,
            IMediator mediator,
            CancellationToken ct) =>
        {
            try
            {
                return Results.Ok(await mediator.Send(
                    new ListIngestionsOrdersQuery(integrationId, pageSize ?? 25, nextToken),
                    ct));
            }
            catch (KeyNotFoundException ex)
            {
                return Results.NotFound(new { error = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                return Results.BadRequest(new { error = ex.Message });
            }
        });

        group.MapGet("/ingestions/orders/{id}", async (
            Guid integrationId,
            string id,
            IMediator mediator,
            CancellationToken ct) =>
        {
            try
            {
                var result = await mediator.Send(new GetIngestionOrderQuery(integrationId, id), ct);
                return result is null ? Results.NotFound() : Results.Ok(result);
            }
            catch (KeyNotFoundException ex)
            {
                return Results.NotFound(new { error = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                return Results.BadRequest(new { error = ex.Message });
            }
        });
    }

    private sealed record EnqueueRequest(string? Kind = null);
}
