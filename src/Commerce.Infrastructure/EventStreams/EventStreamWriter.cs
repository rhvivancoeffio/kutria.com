using System.Text.Json;
using Commerce.Application.Abstracts;

namespace Commerce.Infrastructure.EventStreams;

/// <summary>Thrifty helpers to append logical progress and always close with a terminal event.</summary>
public static class EventStreamWriter
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    public static Task StatusAsync(
        IEventStreamStore store,
        string tenantId,
        string streamId,
        string text,
        object? data = null,
        CancellationToken cancellationToken = default)
        => store.AppendAsync(
            tenantId,
            streamId,
            EventStreamKeys.TypeStatus,
            text,
            data is null ? null : JsonSerializer.Serialize(data, Json),
            cancellationToken);

    public static Task ProgressAsync(
        IEventStreamStore store,
        string tenantId,
        string streamId,
        string text,
        object? data = null,
        CancellationToken cancellationToken = default)
        => store.AppendAsync(
            tenantId,
            streamId,
            EventStreamKeys.TypeProgress,
            text,
            data is null ? null : JsonSerializer.Serialize(data, Json),
            cancellationToken);

    public static Task DoneAsync(
        IEventStreamStore store,
        string tenantId,
        string streamId,
        string? text = null,
        object? data = null,
        CancellationToken cancellationToken = default)
        => store.AppendAsync(
            tenantId,
            streamId,
            EventStreamKeys.TypeDone,
            text,
            data is null ? null : JsonSerializer.Serialize(data, Json),
            cancellationToken);

    public static Task ErrorAsync(
        IEventStreamStore store,
        string tenantId,
        string streamId,
        string text,
        object? data = null,
        CancellationToken cancellationToken = default)
        => store.AppendAsync(
            tenantId,
            streamId,
            EventStreamKeys.TypeError,
            text,
            data is null ? null : JsonSerializer.Serialize(data, Json),
            cancellationToken);
}
