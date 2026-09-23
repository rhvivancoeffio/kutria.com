namespace Commerce.Application.Abstracts;

public static class SessionBuyerProfileKeys
{
    public static string Build(string tenantId, string threadId)
        => $"buyer-profile:{tenantId}:{threadId}";
}

/// <summary>
/// Authoritative buyer session facts for a chat thread (not routing rules).
/// </summary>
public sealed record SessionBuyerProfile(
    string TenantId,
    string ThreadId,
    string? CartId,
    int CartItemCount,
    string? LastViewedProductId,
    string? LastViewedProductName,
    bool HasPendingOrder,
    string? DraftOrderId,
    bool HasShippingAddress,
    DateTimeOffset UpdatedAt)
{
    public static SessionBuyerProfile Empty(string tenantId, string threadId)
        => new(
            TenantId: tenantId,
            ThreadId: threadId,
            CartId: null,
            CartItemCount: 0,
            LastViewedProductId: null,
            LastViewedProductName: null,
            HasPendingOrder: false,
            DraftOrderId: null,
            HasShippingAddress: false,
            UpdatedAt: DateTimeOffset.UtcNow);
}
