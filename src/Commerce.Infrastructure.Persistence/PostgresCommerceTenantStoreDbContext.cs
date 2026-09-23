using Finbuckle.MultiTenant.EntityFrameworkCore.Stores;
using Microsoft.EntityFrameworkCore;
using Commerce.Domain.Tenants;

namespace Commerce.Infrastructure.Persistence;

public interface ICommerceTenantDatabase
{
    DbSet<CommerceTenantInfo> TenantInfo { get; }
    Task MigrateAsync(CancellationToken cancellationToken = default);
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}

public sealed class PostgresCommerceTenantStoreDbContext
    : EFCoreStoreDbContext<CommerceTenantInfo>, ICommerceTenantDatabase
{
    public PostgresCommerceTenantStoreDbContext(DbContextOptions<PostgresCommerceTenantStoreDbContext> options)
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
