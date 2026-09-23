using Carter;
using MediatR;
using Commerce.Application.Features.Observability.ListChatEvents;

namespace Commerce.Api.Modules;

public sealed class ObservabilityModule : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        Map(app.MapGroup("/t/{__tenant__}/observability").WithTags("Observability"));
    }

    private static void Map(RouteGroupBuilder group)
    {
        group.MapGet("/chat-events", async (
            int? pageSize,
            string? nextToken,
            string? threadId,
            string? eventType,
            bool? emptyOnly,
            IMediator mediator,
            CancellationToken ct) =>
        {
            try
            {
                return Results.Ok(await mediator.Send(
                    new ListChatEventsQuery(
                        pageSize ?? 25,
                        nextToken,
                        threadId,
                        eventType,
                        emptyOnly),
                    ct));
            }
            catch (InvalidOperationException ex)
            {
                return Results.BadRequest(new { error = ex.Message });
            }
        });
    }
}
