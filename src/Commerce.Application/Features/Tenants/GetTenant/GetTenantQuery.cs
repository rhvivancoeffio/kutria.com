using Commerce.Application.Common.Abstracts;

namespace Commerce.Application.Features.Tenants.GetTenant;

public sealed record GetTenantQuery(string Identifier) : IQuery<GetTenantResult?>;
