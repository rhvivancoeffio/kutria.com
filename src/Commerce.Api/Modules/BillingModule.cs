using Carter;
using MediatR;
using Commerce.Application.Features.Billing.GetBilling;
using Commerce.Application.Features.Billing.ListInvoices;
using Commerce.Application.Features.Billing.UpdateBillingPlan;

namespace Commerce.Api.Modules;

public sealed class BillingModule : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        Map(app.MapGroup("/t/{__tenant__}/billing").WithTags("Billing"));
    }

    private static void Map(RouteGroupBuilder group)
    {
        group.MapGet("/", async (IMediator mediator, CancellationToken ct) =>
        {
            var result = await mediator.Send(new GetBillingQuery(), ct);
            return result is null ? Results.NotFound() : Results.Ok(result);
        });

        group.MapPut("/plan", async (UpdatePlanRequest body, IMediator mediator, CancellationToken ct) =>
            Results.Ok(await mediator.Send(new UpdateBillingPlanCommand(body.PlanCode), ct)));

        group.MapGet("/invoices", async (IMediator mediator, CancellationToken ct) =>
            Results.Ok(await mediator.Send(new ListInvoicesQuery(), ct)));
    }

    private sealed record UpdatePlanRequest(string PlanCode);
}
