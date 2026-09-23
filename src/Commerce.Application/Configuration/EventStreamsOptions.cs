namespace Commerce.Application.Configuration;

/// <summary>
/// Client wait transport for waitable server event-streams.
/// Configured in appsettings for Api / Worker / Mcp (not Vite).
/// </summary>
public sealed class EventStreamsOptions
{
    public const string SectionName = "EventStreams";

    /// <summary>Sse (default, low-cost) or TableStorage (client short-polls Table-backed events).</summary>
    public string Transport { get; set; } = "Sse";

    /// <summary>Poll interval when <see cref="Transport"/> is TableStorage. Clamped 400–3000.</summary>
    public int PollIntervalMs { get; set; } = 800;

    /// <summary>Soft TTL for stream partitions.</summary>
    public string Ttl { get; set; } = "24:00:00";

    public bool UseTableStorageTransport
        => string.Equals(Transport, "TableStorage", StringComparison.OrdinalIgnoreCase)
           || string.Equals(Transport, "Poll", StringComparison.OrdinalIgnoreCase);

    public int ClampedPollIntervalMs
        => Math.Clamp(PollIntervalMs <= 0 ? 800 : PollIntervalMs, 400, 3000);

    public string NormalizedTransport
        => UseTableStorageTransport ? "TableStorage" : "Sse";
}
