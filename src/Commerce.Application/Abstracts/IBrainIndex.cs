namespace Commerce.Application.Abstracts;

public sealed record BrainPoint(string PointId, string TextToEmbed, IReadOnlyDictionary<string, string> Payload);

public sealed record BrainSearch(
    string CollectionName,
    string TenantId,
    string Text,
    int Limit,
    IReadOnlyDictionary<string, string>? MustMatch);

public sealed record BrainHit(float Score, IReadOnlyDictionary<string, string> Payload);

public interface IBrainIndex
{
    Task UpsertAsync(
        string collectionName,
        IReadOnlyList<BrainPoint> points,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<BrainHit>> SearchAsync(BrainSearch search, CancellationToken cancellationToken = default);

    Task SetPayloadAsync(
        string collectionName,
        IReadOnlyDictionary<string, string> match,
        IReadOnlyDictionary<string, string> payload,
        CancellationToken cancellationToken = default);

    Task DeleteMatchingAsync(
        string collectionName,
        IReadOnlyDictionary<string, string> match,
        IReadOnlyDictionary<string, string>? except,
        CancellationToken cancellationToken = default);
}
