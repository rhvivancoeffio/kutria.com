namespace Commerce.Application.Abstracts;

public interface ICatalogTools
{
    Task<string> SearchSemanticAsync(string tenantId, string query, CancellationToken cancellationToken = default);
    Task<string> GetProductAsync(string productId, CancellationToken cancellationToken = default);
    Task<string> CheckStockAsync(string productId, CancellationToken cancellationToken = default);
    Task<string> AddItemAsync(string productId, int quantity, CancellationToken cancellationToken = default);
    Task<string> RemoveItemAsync(string productId, CancellationToken cancellationToken = default);
    Task<string> GetCartAsync(CancellationToken cancellationToken = default);
}

public interface IOrderTools
{
    Task<string> CreateOrderDraftAsync(CancellationToken cancellationToken = default);
    Task<string> GetOrderTimelineAsync(string orderId, CancellationToken cancellationToken = default);
    Task<string> CheckCarrierAsync(string orderId, CancellationToken cancellationToken = default);
    Task<string> CreateReturnAsync(string orderId, CancellationToken cancellationToken = default);
    Task<string> ReserveStockAsync(string orderId, CancellationToken cancellationToken = default);
    Task<string> CreateLabelAsync(string orderId, CancellationToken cancellationToken = default);
    Task PublishAsync(string eventName, string payload, CancellationToken cancellationToken = default);
}

public interface IKpiTools
{
    Task<string> GetKpiAsync(string name, CancellationToken cancellationToken = default);
    Task<string> GetOrdersDelayedAsync(CancellationToken cancellationToken = default);
    Task<string> GetClaimsBreakdownAsync(CancellationToken cancellationToken = default);
    Task<string> GetMarketplaceHealthAsync(CancellationToken cancellationToken = default);
}
