namespace Commerce.Application.Features.Tenants.ListTenants;

public sealed record ListTenantsResult(IReadOnlyList<TenantListItem> Items);
