namespace Commerce.Domain.Tenants;

/// <summary>
/// How the tenant fulfilled the mandatory store connection step.
/// Shared by all users of the tenant (cross-device / invited members).
/// </summary>
public enum CommerceStoreMode
{
    /// <summary>Mandatory modal not completed yet.</summary>
    None = 0,

    /// <summary>Created native Gravity store — admin Tienda menu (products/categories/brands).</summary>
    Native = 1,

    /// <summary>Connected an external store or API — hide native Tienda admin menu.</summary>
    Connected = 2
}
