namespace Commerce.Application.Abstracts;

/// <summary>
/// Thin write API for tools to patch the current turn's buyer session profile.
/// </summary>
public interface ISessionBuyerProfileUpdater
{
    Task SetCartAsync(
        string? cartId,
        int itemCount,
        CancellationToken cancellationToken = default);

    Task SetLastViewedProductAsync(
        string? productId,
        string? productName,
        CancellationToken cancellationToken = default);

    Task SetCheckoutFlagsAsync(
        bool hasPendingOrder,
        string? draftOrderId = null,
        bool? hasShippingAddress = null,
        CancellationToken cancellationToken = default);
}
