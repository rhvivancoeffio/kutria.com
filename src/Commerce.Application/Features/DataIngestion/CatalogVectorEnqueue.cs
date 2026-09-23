using Commerce.Application.Abstracts;

namespace Commerce.Application.Features.DataIngestion;

public static class CatalogVectorEnqueue
{
    public static bool ShouldEnqueue(
        DataIngestCatalogMeta? meta,
        string contentHash,
        DateTimeOffset? sourceUpdatedOn,
        DateTimeOffset utcNow)
    {
        if (meta is null)
            return true;

        if (!string.Equals(meta.ContentHash, contentHash, StringComparison.OrdinalIgnoreCase))
            return true;

        if (sourceUpdatedOn is not null
            && meta.UpdatedOn is not null
            && sourceUpdatedOn != meta.UpdatedOn)
            return true;

        var status = meta.Status;
        if (string.IsNullOrWhiteSpace(status) || !DataIngestPipelineStatuses.IsKnown(status))
            return true;

        if (string.Equals(status, DataIngestPipelineStatuses.Failed, StringComparison.OrdinalIgnoreCase))
            return IsStale(meta.StatusChangedAt, utcNow, DataIngestPipelineStatuses.PendingStaleAfter);

        if (string.Equals(status, DataIngestPipelineStatuses.Indexed, StringComparison.OrdinalIgnoreCase)
            && string.Equals(meta.ContentHash, contentHash, StringComparison.OrdinalIgnoreCase))
            return false;

        if (string.Equals(status, DataIngestPipelineStatuses.Pending, StringComparison.OrdinalIgnoreCase))
            return IsStale(meta.StatusChangedAt, utcNow, DataIngestPipelineStatuses.PendingStaleAfter);

        if (string.Equals(status, DataIngestPipelineStatuses.Processing, StringComparison.OrdinalIgnoreCase))
            return IsStale(meta.StatusChangedAt, utcNow, DataIngestPipelineStatuses.ProcessingLease);

        return true;
    }

    private static bool IsStale(DateTimeOffset? changedAt, DateTimeOffset utcNow, TimeSpan maxAge)
    {
        if (changedAt is null)
            return true;
        return utcNow - changedAt.Value >= maxAge;
    }
}
