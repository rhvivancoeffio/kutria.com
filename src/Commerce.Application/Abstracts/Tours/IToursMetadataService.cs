namespace Commerce.Application.Abstracts.Tours;

/// <summary>
/// Reads tour metadata from YAML seed files.
/// </summary>
public interface IToursMetadataService
{
    /// <summary>
    /// Gets a tour by ID (e.g. "onboarding").
    /// </summary>
    Task<TourDto?> GetTourAsync(string tourId, CancellationToken cancellationToken = default);
}

/// <summary>
/// Tour metadata from YAML.
/// </summary>
public record TourDto(
    string Id,
    string Name,
    string Description,
    IReadOnlyList<TourStepDto> Steps
);

/// <summary>
/// A single step in a tour.
/// </summary>
public record TourStepDto(
    string Id,
    int Order,
    string Title,
    string Description,
    string Route,
    string RouteLabel
);
