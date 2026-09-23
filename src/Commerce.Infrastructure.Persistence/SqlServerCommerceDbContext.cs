using Finbuckle.MultiTenant.Abstractions;
using Microsoft.EntityFrameworkCore;

namespace Commerce.Infrastructure.Persistence;

public sealed class SqlServerCommerceDbContext : CommerceDbContextBase
{
    public SqlServerCommerceDbContext(
        IMultiTenantContextAccessor multiTenantContextAccessor,
        DbContextOptions<SqlServerCommerceDbContext> options)
        : base(multiTenantContextAccessor, options)
    {
    }
}
