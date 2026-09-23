using Commerce.Domain.Tenants;

namespace Commerce.Application.Features.Integrations;

/// <summary>
/// Updates tenant <see cref="CommerceStoreMode"/> when the first store/API integration is created.
/// Native Gravity never downgrades to Connected.
/// </summary>
public static class TenantCommerceStoreModeUpdater
{
    public const string NativeGravityProvider = "Gravity";

    public static CommerceStoreMode ResolveNextMode(CommerceStoreMode current, string provider)
    {
        if (current == CommerceStoreMode.Native)
            return CommerceStoreMode.Native;

        if (string.Equals(provider, NativeGravityProvider, StringComparison.OrdinalIgnoreCase))
            return CommerceStoreMode.Native;

        if (current == CommerceStoreMode.None)
            return CommerceStoreMode.Connected;

        return current;
    }
}
