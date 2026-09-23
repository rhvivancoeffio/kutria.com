using Commerce.Domain.Common;

namespace Commerce.Domain.Auth;

/// <summary>
/// Account owner for a Finbuckle tenant. Not query-filtered: signup runs before a tenant is resolved.
/// </summary>
public class TenantOwner : BaseEntity
{
    public string TenantId { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
}
