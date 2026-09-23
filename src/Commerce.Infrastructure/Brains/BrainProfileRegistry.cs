using Commerce.Application.Abstracts;

namespace Commerce.Infrastructure.Brains;

public sealed class BrainProfileRegistry(IEnumerable<IBrainProfile> profiles) : IBrainProfileRegistry
{
    public IBrainProfile Get(string key)
        => profiles.FirstOrDefault(profile => string.Equals(profile.Key, key, StringComparison.OrdinalIgnoreCase))
            ?? throw new InvalidOperationException($"No brain profile is registered for '{key}'.");
}
