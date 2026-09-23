using Carter;
using MediatR;
using Commerce.Application.Features.Tenants.CreateTenant;
using Commerce.Application.Features.Tenants.GetTenant;
using Commerce.Application.Features.Tenants.ListTenants;
using Commerce.Application.Features.Tenants.UpdateTenant;

namespace Commerce.Api.Modules;

public sealed class TenantsModule : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        var admin = app.MapGroup("/tenants").WithTags("Tenants");
        admin.MapGet("/", async (IMediator mediator, CancellationToken ct) =>
            Results.Ok(await mediator.Send(new ListTenantsQuery(), ct)));
        admin.MapPost("/", async (CreateTenantRequest body, IMediator mediator, CancellationToken ct) =>
        {
            var result = await mediator.Send(new CreateTenantCommand(body.Identifier, body.Name), ct);
            return Results.Created($"/tenants/{result.Identifier}", result);
        });
        admin.MapGet("/{identifier}", async (string identifier, IMediator mediator, CancellationToken ct) =>
        {
            var result = await mediator.Send(new GetTenantQuery(identifier), ct);
            return result is null ? Results.NotFound() : Results.Ok(result);
        });
        admin.MapPut("/{identifier}", async (string identifier, UpdateTenantRequest body, IMediator mediator, CancellationToken ct) =>
            Results.Ok(await mediator.Send(new UpdateTenantCommand(identifier, body.Name), ct)));
    }

    private sealed record CreateTenantRequest(string Identifier, string Name);
    private sealed record UpdateTenantRequest(string Name);
}
