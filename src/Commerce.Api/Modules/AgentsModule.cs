using Carter;
using MediatR;
using Commerce.Application.Abstracts;
using Commerce.Application.Features.Agents.GetAgent;
using Commerce.Application.Features.Agents.ListAgents;
using Commerce.Application.Features.Agents.ResetAgentDefinition;
using Commerce.Application.Features.Agents.SaveAgentDefinition;

namespace Commerce.Api.Modules;

public sealed class AgentsModule : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        Map(app.MapGroup("/t/{__tenant__}/agents").WithTags("Agents"));
    }

    private static void Map(RouteGroupBuilder group)
    {
        group.MapGet("/", async (IMediator mediator, CancellationToken ct) =>
            Results.Ok(await mediator.Send(new ListAgentsQuery(), ct)));

        group.MapGet("/{key}", async (string key, IMediator mediator, CancellationToken ct) =>
        {
            var result = await mediator.Send(new GetAgentQuery(key), ct);
            return result is null ? Results.NotFound() : Results.Ok(result);
        });

        group.MapPut("/{key}", async (string key, HttpRequest request, IMediator mediator, CancellationToken ct) =>
        {
            using var reader = new StreamReader(request.Body);
            var yaml = await reader.ReadToEndAsync(ct);
            try
            {
                var result = await mediator.Send(new SaveAgentDefinitionCommand(key, yaml), ct);
                return Results.Ok(result);
            }
            catch (AgentDefinitionRejectedException ex)
            {
                return Results.BadRequest(new { error = ex.Message });
            }
        });

        group.MapDelete("/{key}", async (string key, IMediator mediator, CancellationToken ct) =>
            Results.Ok(await mediator.Send(new ResetAgentDefinitionCommand(key), ct)));
    }
}
