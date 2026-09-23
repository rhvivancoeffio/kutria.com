namespace Commerce.Infrastructure.Vectors.Common;

public static class RrfFusion
{
    public const int DefaultK = 60;

    public static IReadOnlyList<(string Id, float Score)> Merge(
        IReadOnlyList<IReadOnlyList<(string Id, float Score)>> rankedLists,
        int limit,
        int k = DefaultK)
    {
        var scores = new Dictionary<string, float>(StringComparer.OrdinalIgnoreCase);
        foreach (var list in rankedLists)
        {
            for (var rank = 0; rank < list.Count; rank++)
            {
                var id = list[rank].Id;
                if (string.IsNullOrWhiteSpace(id))
                    continue;
                var contribution = 1f / (k + rank + 1);
                scores[id] = scores.TryGetValue(id, out var existing) ? existing + contribution : contribution;
            }
        }

        return scores
            .OrderByDescending(x => x.Value)
            .Take(Math.Max(1, limit))
            .Select(x => (x.Key, x.Value))
            .ToList();
    }
}
