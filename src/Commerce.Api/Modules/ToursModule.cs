using Carter;
using Commerce.Application.Abstracts.Tours;

namespace Commerce.Api.Modules;

/// <summary>
/// Tour metadata (onboarding, etc.) from YAML seed. Progress lives in the frontend localStorage.
/// </summary>
public sealed class ToursModule : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        Map(app.MapGroup("/t/{__tenant__}/tours").WithTags("Tours"));
    }

    private static void Map(RouteGroupBuilder group)
    {
        group.MapGet("/{tourId}", async (
            string tourId,
            IToursMetadataService toursService,
            CancellationToken ct) =>
        {
            var tour = await toursService.GetTourAsync(tourId, ct);
            return tour is null ? Results.NotFound() : Results.Ok(tour);
        })
        .WithName("GetTour")
        .WithSummary("Get tour metadata by ID")
        .WithDescription(
            "Returns tour steps from YAML seed (e.g. onboarding). Progress is stored in frontend localStorage per account.");
    }
}
