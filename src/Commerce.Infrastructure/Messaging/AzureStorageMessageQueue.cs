using System.Text;
using Azure.Storage.Queues;
using Azure.Storage.Queues.Models;
using Commerce.Application.Abstracts;

namespace Commerce.Infrastructure.Messaging;

public sealed class AzureStorageMessageQueue : IMessageQueue
{
    private readonly string _connectionString;

    public AzureStorageMessageQueue(string connectionString)
    {
        _connectionString = connectionString;
    }

    public async Task SendAsync(string queueName, ReadOnlyMemory<byte> body, CancellationToken cancellationToken = default)
    {
        var client = Client(queueName);
        await client.CreateIfNotExistsAsync(cancellationToken: cancellationToken);
        await client.SendMessageAsync(Encoding.UTF8.GetString(body.Span), cancellationToken);
    }

    public async Task<QueuedMessage?> ReceiveAsync(
        string queueName,
        CancellationToken cancellationToken = default,
        TimeSpan? visibilityTimeout = null)
    {
        var client = Client(queueName);
        await client.CreateIfNotExistsAsync(cancellationToken: cancellationToken);
        var visibility = visibilityTimeout ?? TimeSpan.FromSeconds(30);
        QueueMessage[] messages = await client.ReceiveMessagesAsync(1, visibility, cancellationToken);
        var message = messages.FirstOrDefault();
        if (message is null)
        {
            return null;
        }

        return new QueuedMessage(
            message.MessageId,
            message.PopReceipt,
            message.Body.ToArray(),
            message.DequeueCount);
    }

    public async Task AcknowledgeAsync(string queueName, string messageId, string popReceipt, CancellationToken cancellationToken = default)
    {
        var client = Client(queueName);
        await client.DeleteMessageAsync(messageId, popReceipt, cancellationToken);
    }

    public async Task ReleaseForRetryAsync(
        string queueName,
        string messageId,
        string popReceipt,
        CancellationToken cancellationToken = default)
    {
        var client = Client(queueName);
        await client.UpdateMessageAsync(
            messageId,
            popReceipt,
            visibilityTimeout: TimeSpan.Zero,
            cancellationToken: cancellationToken);
    }

    private QueueClient Client(string queueName)
        => new(_connectionString, Normalize(queueName));

    private static string Normalize(string queueName)
    {
        var name = queueName.Trim().ToLowerInvariant().Replace('_', '-');
        return name.Length > 63 ? name[..63] : name;
    }
}
