namespace Commerce.Application.Abstracts;

public sealed record QueuedMessage(
    string MessageId,
    string PopReceipt,
    ReadOnlyMemory<byte> Body,
    long DequeueCount = 1);

public interface IMessageQueue
{
    Task SendAsync(string queueName, ReadOnlyMemory<byte> body, CancellationToken cancellationToken = default);

    Task<QueuedMessage?> ReceiveAsync(
        string queueName,
        CancellationToken cancellationToken = default,
        TimeSpan? visibilityTimeout = null);

    Task AcknowledgeAsync(string queueName, string messageId, string popReceipt, CancellationToken cancellationToken = default);

    Task ReleaseForRetryAsync(
        string queueName,
        string messageId,
        string popReceipt,
        CancellationToken cancellationToken = default);
}
