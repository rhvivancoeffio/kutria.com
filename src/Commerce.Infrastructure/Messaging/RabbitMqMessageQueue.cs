using System.Collections.Concurrent;
using RabbitMQ.Client;
using Commerce.Application.Abstracts;

namespace Commerce.Infrastructure.Messaging;

public sealed class RabbitMqMessageQueue : IMessageQueue, IDisposable
{
    private readonly ConnectionFactory _factory;
    private readonly object _gate = new();
    private readonly ConcurrentDictionary<string, ulong> _tags = new(StringComparer.Ordinal);
    private IConnection? _connection;
    private IModel? _channel;

    public RabbitMqMessageQueue(string amqpUri)
    {
        _factory = new ConnectionFactory
        {
            Uri = new Uri(amqpUri),
            DispatchConsumersAsync = true
        };
    }

    public Task SendAsync(string queueName, ReadOnlyMemory<byte> body, CancellationToken cancellationToken = default)
    {
        lock (_gate)
        {
            var channel = Channel();
            channel.QueueDeclare(queueName, durable: true, exclusive: false, autoDelete: false);
            var properties = channel.CreateBasicProperties();
            properties.Persistent = true;
            channel.BasicPublish("", queueName, properties, body.ToArray());
        }

        return Task.CompletedTask;
    }

    public Task<QueuedMessage?> ReceiveAsync(
        string queueName,
        CancellationToken cancellationToken = default,
        TimeSpan? visibilityTimeout = null)
    {
        _ = visibilityTimeout;
        lock (_gate)
        {
            var channel = Channel();
            channel.QueueDeclare(queueName, durable: true, exclusive: false, autoDelete: false);
            var result = channel.BasicGet(queueName, autoAck: false);
            if (result is null)
            {
                return Task.FromResult<QueuedMessage?>(null);
            }

            var receipt = result.DeliveryTag.ToString();
            _tags[receipt] = result.DeliveryTag;
            var message = new QueuedMessage(receipt, receipt, result.Body.ToArray());
            return Task.FromResult<QueuedMessage?>(message);
        }
    }

    public Task AcknowledgeAsync(string queueName, string messageId, string popReceipt, CancellationToken cancellationToken = default)
    {
        lock (_gate)
        {
            if (_tags.TryRemove(popReceipt, out var tag))
            {
                Channel().BasicAck(tag, multiple: false);
            }
        }

        return Task.CompletedTask;
    }

    public Task ReleaseForRetryAsync(
        string queueName,
        string messageId,
        string popReceipt,
        CancellationToken cancellationToken = default)
    {
        lock (_gate)
        {
            if (_tags.TryRemove(popReceipt, out var tag))
            {
                Channel().BasicNack(tag, multiple: false, requeue: true);
            }
        }

        return Task.CompletedTask;
    }

    public void Dispose()
    {
        lock (_gate)
        {
            _channel?.Dispose();
            _connection?.Dispose();
        }
    }

    private IModel Channel()
    {
        if (_channel is { IsOpen: true })
        {
            return _channel;
        }

        _connection ??= _factory.CreateConnection();
        _channel = _connection.CreateModel();
        return _channel;
    }
}
