using Commerce.Application.Abstracts;

namespace Commerce.Infrastructure.Agents.Tools;

public sealed class OrderTools(IMessageQueue queue) : IOrderTools
{
    public Task<string> CreateOrderDraftAsync(CancellationToken cancellationToken = default)
        => NotImplemented(nameof(CreateOrderDraftAsync));

    public Task<string> GetOrderTimelineAsync(string orderId, CancellationToken cancellationToken = default)
        => NotImplemented(nameof(GetOrderTimelineAsync));

    public Task<string> CheckCarrierAsync(string orderId, CancellationToken cancellationToken = default)
        => NotImplemented(nameof(CheckCarrierAsync));

    public Task<string> CreateReturnAsync(string orderId, CancellationToken cancellationToken = default)
        => NotImplemented(nameof(CreateReturnAsync));

    public Task<string> ReserveStockAsync(string orderId, CancellationToken cancellationToken = default)
        => Task.FromResult($"reserve_stock acknowledged for {orderId}. Stock domain is not in this slice.");

    public Task<string> CreateLabelAsync(string orderId, CancellationToken cancellationToken = default)
        => Task.FromResult($"create_label acknowledged for {orderId}. Carrier domain is not in this slice.");

    public async Task PublishAsync(string eventName, string payload, CancellationToken cancellationToken = default)
    {
        var body = System.Text.Encoding.UTF8.GetBytes(payload);
        await queue.SendAsync(eventName, body, cancellationToken);
    }

    private static Task<string> NotImplemented(string name)
        => Task.FromResult($"{name} is not implemented. The commerce domain is not in this slice.");
}
