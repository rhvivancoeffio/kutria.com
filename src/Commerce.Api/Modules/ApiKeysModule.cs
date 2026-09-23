using Carter;
using MediatR;
using Commerce.Application.Features.ApiKeys.CreateApiKey;
using Commerce.Application.Features.ApiKeys.DeleteApiKey;
using Commerce.Application.Features.ApiKeys.ListApiKeys;
using Commerce.Application.Features.ApiKeys.RevokeApiKey;

namespace Commerce.Api.Modules;

public sealed class ApiKeysModule : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        Map(app.MapGroup("/t/{__tenant__}/api-keys").WithTags("ApiKeys"));
    }

    private static void Map(RouteGroupBuilder group)
    {
        group.MapGet("/", async (IMediator mediator, CancellationToken ct) =>
        {
            var result = await mediator.Send(new ListApiKeysQuery(), ct);
            return Results.Ok(result.Items);
        });

        group.MapPost("/", async (CreateApiKeyRequest body, IMediator mediator, CancellationToken ct) =>
        {
            var result = await mediator.Send(new CreateApiKeyCommand(body.Name), ct);
            return Results.Created($"/api-keys/{result.Id}", result);
        });

        group.MapPatch("/{id:guid}/revoke", async (Guid id, IMediator mediator, CancellationToken ct) =>
            Results.Ok(await mediator.Send(new RevokeApiKeyCommand(id), ct)));

        group.MapDelete("/{id:guid}", async (Guid id, IMediator mediator, CancellationToken ct) =>
            Results.Ok(await mediator.Send(new DeleteApiKeyCommand(id), ct)));
    }

    private sealed record CreateApiKeyRequest(string Name);
}
