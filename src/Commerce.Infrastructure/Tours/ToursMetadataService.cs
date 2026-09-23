using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Commerce.Application.Abstracts;
using Commerce.Application.Abstracts.Tours;

namespace Commerce.Infrastructure.Tours;

/// <summary>
/// Reads tour metadata from YAML seed files under <c>data/seed/tours/</c>.
/// </summary>
public sealed class ToursMetadataService : IToursMetadataService
{
    private const string CacheKeyPrefix = "Tours:";
    private static readonly TimeSpan CacheExpiration = TimeSpan.FromHours(1);

    private readonly IMemoryCache _cache;
    private readonly ILogger<ToursMetadataService> _logger;
    private readonly IYamlMetadataService _yaml;

    public ToursMetadataService(
        IMemoryCache cache,
        ILogger<ToursMetadataService> logger,
        IYamlMetadataService yaml)
    {
        _cache = cache ?? throw new ArgumentNullException(nameof(cache));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _yaml = yaml ?? throw new ArgumentNullException(nameof(yaml));
    }

    public Task<TourDto?> GetTourAsync(string tourId, CancellationToken cancellationToken = default)
    {
        var cacheKey = CacheKeyPrefix + tourId;
        return _cache.GetOrCreateAsync(cacheKey, async entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = CacheExpiration;

            var root = await _yaml.DeserializeAsync<TourYamlRoot>($"seed/tours/{tourId}.yaml", cancellationToken);
            if (root?.Id == null)
            {
                _logger.LogWarning("Tour file missing or invalid: seed/tours/{TourId}.yaml", tourId);
                return null;
            }

            var steps = (root.Steps ?? [])
                .OrderBy(s => s.Order)
                .Select(s => new TourStepDto(
                    s.Id ?? string.Empty,
                    s.Order,
                    s.Title ?? string.Empty,
                    s.Description ?? string.Empty,
                    s.Route ?? string.Empty,
                    s.RouteLabel ?? "Ir"
                ))
                .ToList();

            return new TourDto(
                root.Id,
                root.Name ?? root.Id,
                root.Description ?? string.Empty,
                steps
            );
        })!;
    }

    private sealed class TourYamlRoot
    {
        public string? Id { get; set; }
        public string? Name { get; set; }
        public string? Description { get; set; }
        public List<TourStepYaml>? Steps { get; set; }
    }

    private sealed class TourStepYaml
    {
        public string? Id { get; set; }
        public int Order { get; set; }
        public string? Title { get; set; }
        public string? Description { get; set; }
        public string? Route { get; set; }
        public string? RouteLabel { get; set; }
    }
}
