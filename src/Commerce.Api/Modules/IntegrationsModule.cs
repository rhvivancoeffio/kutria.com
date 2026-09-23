using System.Text.Json;
using Carter;
using MediatR;
using Commerce.Application.Features.Integrations.CreateIntegration;
using Commerce.Application.Features.Integrations.DeleteIntegration;
using Commerce.Application.Features.Integrations.GetAvailableIntegrations;
using Commerce.Application.Features.Integrations.GetIntegration;
using Commerce.Application.Features.Integrations.ListIntegrations;
using Commerce.Application.Features.Integrations.UpdateIntegration;

namespace Commerce.Api.Modules;

public sealed class IntegrationsModule : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        Map(app.MapGroup("/t/{__tenant__}/integrations").WithTags("Integrations"));
    }

    private static void Map(RouteGroupBuilder group)
    {
        group.MapGet("/available", async (IMediator mediator, CancellationToken ct) =>
            Results.Ok(await mediator.Send(new GetAvailableIntegrationsQuery(), ct)));

        group.MapGet("/", async (IMediator mediator, CancellationToken ct) =>
        {
            var result = await mediator.Send(new ListIntegrationsQuery(), ct);
            return Results.Ok(result.Items);
        });

        group.MapGet("/{id:guid}", async (
            Guid id,
            bool? revealSensitiveSettings,
            IMediator mediator,
            CancellationToken ct) =>
        {
            var result = await mediator.Send(
                new GetIntegrationQuery(id, revealSensitiveSettings ?? false),
                ct);
            return result is null ? Results.NotFound() : Results.Ok(result);
        });

        group.MapPost("/", async (CreateIntegrationRequest body, IMediator mediator, CancellationToken ct) =>
        {
            var result = await mediator.Send(
                new CreateIntegrationCommand(body.Provider, body.Name, body.Settings),
                ct);
            return Results.Created($"/integrations/{result.Id}", result);
        });

        group.MapPut("/{id:guid}", async (Guid id, UpdateIntegrationRequest body, IMediator mediator, CancellationToken ct) =>
            Results.Ok(await mediator.Send(
                new UpdateIntegrationCommand(id, body.Name, body.IsActive, body.Settings),
                ct)));

        group.MapDelete("/{id:guid}", async (Guid id, IMediator mediator, CancellationToken ct) =>
            Results.Ok(await mediator.Send(new DeleteIntegrationCommand(id), ct)));
    }

    private sealed record CreateIntegrationRequest(
        string Provider,
        string Name,
        Dictionary<string, JsonElement>? Settings = null);

    private sealed record UpdateIntegrationRequest(
        string? Name = null,
        bool? IsActive = null,
        Dictionary<string, JsonElement>? Settings = null);
}
