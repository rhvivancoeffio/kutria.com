namespace Commerce.Application.Abstracts;

/// <summary>
/// Controlled cart/session errors for agent tools (missing cart, etc.).
/// Not a transport failure — tools should return this to the model without retry storms.
/// </summary>
public sealed class CartException : Exception
{
    public const string MissingCart = "cart_missing";
    public const string MissingSession = "cart_session_missing";
    public const string EmptyItems = "cart_empty_items";
    public const string SellerMissing = "seller_missing";

    public string Code { get; }

    public CartException(string code, string message) : base(message)
    {
        Code = code;
    }
}
