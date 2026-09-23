using Finbuckle.MultiTenant.Abstractions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Commerce.Domain.Tenants;

namespace Commerce.Infrastructure.Persistence;

public sealed class PostgresCommerceDbContextDesignTimeFactory : IDesignTimeDbContextFactory<PostgresCommerceDbContext>
{
    public PostgresCommerceDbContext CreateDbContext(string[] args)
    {
        var configuration = DesignTimeConfiguration.Build(args);
        var connectionString = DesignTimeConfiguration.ResolveConnectionString(configuration, "PostgreSQL");
        var options = new DbContextOptionsBuilder<PostgresCommerceDbContext>()
            .UseNpgsql(connectionString, npgsql =>
            {
                npgsql.MigrationsAssembly(typeof(PostgresCommerceDbContext).Assembly.FullName);
                npgsql.MigrationsHistoryTable("__EFMigrationsHistory_App");
            })
            .Options;

        return new PostgresCommerceDbContext(DesignTimeAccessors.Create(), options);
    }
}

public sealed class SqlServerCommerceDbContextDesignTimeFactory : IDesignTimeDbContextFactory<SqlServerCommerceDbContext>
{
    public SqlServerCommerceDbContext CreateDbContext(string[] args)
    {
        var configuration = DesignTimeConfiguration.Build(args);
        var connectionString = DesignTimeConfiguration.ResolveConnectionString(configuration, "SqlServer");
        var options = new DbContextOptionsBuilder<SqlServerCommerceDbContext>()
            .UseSqlServer(connectionString, sql =>
            {
                sql.MigrationsAssembly(typeof(SqlServerCommerceDbContext).Assembly.FullName);
                sql.MigrationsHistoryTable("__EFMigrationsHistory_App");
            })
            .Options;

        return new SqlServerCommerceDbContext(DesignTimeAccessors.Create(), options);
    }
}

public sealed class PostgresCommerceTenantStoreDbContextDesignTimeFactory
    : IDesignTimeDbContextFactory<PostgresCommerceTenantStoreDbContext>
{
    public PostgresCommerceTenantStoreDbContext CreateDbContext(string[] args)
    {
        var configuration = DesignTimeConfiguration.Build(args);
        var connectionString = DesignTimeConfiguration.ResolveConnectionString(configuration, "PostgreSQL");
        var options = new DbContextOptionsBuilder<PostgresCommerceTenantStoreDbContext>()
            .UseNpgsql(connectionString, npgsql =>
            {
                npgsql.MigrationsAssembly(typeof(PostgresCommerceTenantStoreDbContext).Assembly.FullName);
                npgsql.MigrationsHistoryTable("__EFMigrationsHistory_TenantStore");
            })
            .Options;

        return new PostgresCommerceTenantStoreDbContext(options);
    }
}

public sealed class SqlServerCommerceTenantStoreDbContextDesignTimeFactory
    : IDesignTimeDbContextFactory<SqlServerCommerceTenantStoreDbContext>
{
    public SqlServerCommerceTenantStoreDbContext CreateDbContext(string[] args)
    {
        var configuration = DesignTimeConfiguration.Build(args);
        var connectionString = DesignTimeConfiguration.ResolveConnectionString(configuration, "SqlServer");
        var options = new DbContextOptionsBuilder<SqlServerCommerceTenantStoreDbContext>()
            .UseSqlServer(connectionString, sql =>
            {
                sql.MigrationsAssembly(typeof(SqlServerCommerceTenantStoreDbContext).Assembly.FullName);
                sql.MigrationsHistoryTable("__EFMigrationsHistory_TenantStore");
            })
            .Options;

        return new SqlServerCommerceTenantStoreDbContext(options);
    }
}

internal static class DesignTimeAccessors
{
    public static IMultiTenantContextAccessor Create()
        => new StaticMultiTenantContextAccessor<CommerceTenantInfo>(
            new CommerceTenantInfo { Id = "design", Identifier = "design", Name = "Design" });
}
