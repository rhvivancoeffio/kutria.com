using Finbuckle.MultiTenant.Abstractions;

namespace Commerce.Domain.Tenants;

public class CommerceTenantInfo : ITenantInfo
{
    public string Id { get; set; } = string.Empty;
    public string Identifier { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Native Gravity store vs connected external store/API. Drives admin Tienda menu visibility.
    /// </summary>
    public CommerceStoreMode CommerceStoreMode { get; set; } = CommerceStoreMode.None;
}
