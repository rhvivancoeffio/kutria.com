using System.Collections.Concurrent;
using Commerce.Application.Abstracts;

namespace Commerce.Infrastructure.Messaging;

public sealed class InMemoryMessageQueue : IMessageQueue
{
    private readonly ConcurrentDictionary<string, ConcurrentQueue<QueuedMessage>> _queues = new(StringComparer.OrdinalIgnoreCase);
    private readonly ConcurrentDictionary<string, QueuedMessage> _inflight = new(StringComparer.Ordinal);

    public Task SendAsync(string queueName, ReadOnlyMemory<byte> body, CancellationToken cancellationToken = default)
    {
        var id = Guid.NewGuid().ToString("N");
        var message = new QueuedMessage(id, id, body.ToArray());
        _queues.GetOrAdd(queueName, _ => new ConcurrentQueue<QueuedMessage>()).Enqueue(message);
        return Task.CompletedTask;
    }

    public Task<QueuedMessage?> ReceiveAsync(
        string queueName,
        CancellationToken cancellationToken = default,
        TimeSpan? visibilityTimeout = null)
    {
        _ = visibilityTimeout;
        if (!_queues.TryGetValue(queueName, out var queue) || !queue.TryDequeue(out var message))
        {
            return Task.FromResult<QueuedMessage?>(null);
        }

        _inflight[message.PopReceipt] = message;
        return Task.FromResult<QueuedMessage?>(message);
    }

    public Task AcknowledgeAsync(string queueName, string messageId, string popReceipt, CancellationToken cancellationToken = default)
    {
        _inflight.TryRemove(popReceipt, out _);
        return Task.CompletedTask;
    }

    public Task ReleaseForRetryAsync(
        string queueName,
        string messageId,
        string popReceipt,
        CancellationToken cancellationToken = default)
    {
        if (_inflight.TryRemove(popReceipt, out var message))
        {
            var retried = message with { DequeueCount = message.DequeueCount + 1 };
            _queues.GetOrAdd(queueName, _ => new ConcurrentQueue<QueuedMessage>()).Enqueue(retried);
        }

        return Task.CompletedTask;
    }
}
