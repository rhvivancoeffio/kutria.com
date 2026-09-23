namespace Commerce.Application.Abstracts;

/// <summary>One progress event in a server-side waitable stream (any feature).</summary>
public sealed record StreamEvent(string Type, string? Text, string? Data, string RowKey);

public sealed record StreamEventListResult(
    IReadOnlyList<StreamEvent> Items,
    string? NextAfter,
    bool Completed);

/// <summary>
/// Low-cost durable buffer for feature progress. Clients wait via SSE and/or short-poll.
/// Stop condition: <c>done</c>/<c>error</c>/<c>cancelled</c> or <see cref="StreamEventListResult.Completed"/>.
/// </summary>
public interface IEventStreamStore
{
    /// <summary>Creates a new process partition and returns its id (processId / streamId).</summary>
    Task<string> StartProcessAsync(
        string tenantId,
        string? kind = null,
        string? data = null,
        CancellationToken cancellationToken = default);

    Task<string> AppendAsync(
        string tenantId,
        string streamId,
        string type,
        string? text = null,
        string? data = null,
        CancellationToken cancellationToken = default);

    Task<StreamEventListResult> ListSinceAsync(
        string tenantId,
        string streamId,
        string? afterRowKey,
        int take = 50,
        CancellationToken cancellationToken = default);

    Task DeleteStreamAsync(
        string tenantId,
        string streamId,
        CancellationToken cancellationToken = default);

    /// <summary>Streams due for deletion (GC worker). Empty when unsupported.</summary>
    Task<IReadOnlyList<(string TenantId, string StreamId)>> ListDueForDeleteAsync(
        DateTimeOffset utcNow,
        int take = 50,
        CancellationToken cancellationToken = default);
}

public static class EventStreamKeys
{
    public const string MetaRowKey = "__meta";
    public const string TypeStarted = "started";
    public const string TypeProgress = "progress";
    public const string TypeStatus = "status";
    public const string TypeDone = "done";
    public const string TypeError = "error";
    public const string TypeCancelled = "cancelled";

    public static string Partition(string tenantId, string streamId)
        => $"{tenantId.Trim()}:{streamId.Trim()}";

    public static string NewRowKey(long seq)
        => $"{DateTimeOffset.UtcNow.UtcTicks:D19}_{seq:D6}";

    /// <summary>Deterministic stream id for a feature scope (e.g. policyeval + job guid).</summary>
    public static string ForScope(string scope, string id)
        => $"{scope.Trim().ToLowerInvariant()}_{id.Trim()}";

    public static string ForPolicyEval(Guid jobId)
        => ForScope("policyeval", jobId.ToString("N"));

    public static bool IsTerminal(string? type)
        => string.Equals(type, TypeDone, StringComparison.OrdinalIgnoreCase)
           || string.Equals(type, TypeError, StringComparison.OrdinalIgnoreCase)
           || string.Equals(type, TypeCancelled, StringComparison.OrdinalIgnoreCase);

    public static bool TrySplitPartition(string partition, out string tenantId, out string streamId)
    {
        tenantId = string.Empty;
        streamId = string.Empty;
        var i = partition.IndexOf(':');
        if (i <= 0 || i >= partition.Length - 1)
            return false;
        tenantId = partition[..i];
        streamId = partition[(i + 1)..];
        return true;
    }
}
