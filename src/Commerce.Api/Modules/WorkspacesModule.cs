using Carter;
using MediatR;
using Commerce.Application.Features.Workspaces.CreateWorkspace;
using Commerce.Application.Features.Workspaces.DeleteWorkspace;
using Commerce.Application.Features.Workspaces.EnsureDefaultWorkspaces;
using Commerce.Application.Features.Workspaces.GetWorkspace;
using Commerce.Application.Features.Workspaces.ListWorkspaces;
using Commerce.Application.Features.Workspaces.UpdateWorkspace;
using Commerce.Domain.Workspaces;

namespace Commerce.Api.Modules;

public sealed class WorkspacesModule : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        Map(app.MapGroup("/t/{__tenant__}/workspaces").WithTags("Workspaces"));
    }

    private static void Map(RouteGroupBuilder group)
    {
        group.MapGet("/", async (IMediator mediator, CancellationToken ct) =>
            Results.Ok(await mediator.Send(new ListWorkspacesQuery(), ct)));

        group.MapGet("/{id:guid}", async (Guid id, IMediator mediator, CancellationToken ct) =>
        {
            var result = await mediator.Send(new GetWorkspaceQuery(id), ct);
            return result is null ? Results.NotFound() : Results.Ok(result);
        });

        group.MapPost("/", async (CreateWorkspaceRequest body, IMediator mediator, CancellationToken ct) =>
        {
            var result = await mediator.Send(
                new CreateWorkspaceCommand(body.Name, body.EnvironmentKind, body.IsDefault),
                ct);
            return Results.Created($"/workspaces/{result.Id}", result);
        });

        group.MapPut("/{id:guid}", async (Guid id, UpdateWorkspaceRequest body, IMediator mediator, CancellationToken ct) =>
            Results.Ok(await mediator.Send(
                new UpdateWorkspaceCommand(id, body.Name, body.EnvironmentKind, body.IsDefault),
                ct)));

        group.MapDelete("/{id:guid}", async (Guid id, IMediator mediator, CancellationToken ct) =>
            Results.Ok(await mediator.Send(new DeleteWorkspaceCommand(id), ct)));

        group.MapPost("/ensure-defaults", async (IMediator mediator, CancellationToken ct) =>
            Results.Ok(await mediator.Send(new EnsureDefaultWorkspacesCommand(), ct)));
    }

    private sealed record CreateWorkspaceRequest(
        string Name,
        WorkspaceEnvironment EnvironmentKind = WorkspaceEnvironment.Sandbox,
        bool IsDefault = false);

    private sealed record UpdateWorkspaceRequest(
        string Name,
        WorkspaceEnvironment EnvironmentKind,
        bool IsDefault);
}
