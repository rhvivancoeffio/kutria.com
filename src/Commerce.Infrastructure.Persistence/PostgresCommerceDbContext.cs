using Finbuckle.MultiTenant.Abstractions;
using Microsoft.EntityFrameworkCore;

namespace Commerce.Infrastructure.Persistence;

public sealed class PostgresCommerceDbContext : CommerceDbContextBase
{
    public PostgresCommerceDbContext(
        IMultiTenantContextAccessor multiTenantContextAccessor,
        DbContextOptions<PostgresCommerceDbContext> options)
        : base(multiTenantContextAccessor, options)
    {
    }
}
