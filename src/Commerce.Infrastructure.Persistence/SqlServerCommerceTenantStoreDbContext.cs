using Finbuckle.MultiTenant.EntityFrameworkCore.Stores;
using Microsoft.EntityFrameworkCore;
using Commerce.Domain.Tenants;

namespace Commerce.Infrastructure.Persistence;

public sealed class SqlServerCommerceTenantStoreDbContext
    : EFCoreStoreDbContext<CommerceTenantInfo>, ICommerceTenantDatabase
{
    public SqlServerCommerceTenantStoreDbContext(DbContextOptions<SqlServerCommerceTenantStoreDbContext> options)
        : base(options)
    {
    }

    public Task MigrateAsync(CancellationToken cancellationToken = default)
        => Database.MigrateAsync(cancellationToken);

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        => base.SaveChangesAsync(cancellationToken);

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.Entity<CommerceTenantInfo>().ToTable("Tenants");
    }
}
