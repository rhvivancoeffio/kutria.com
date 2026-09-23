using Finbuckle.MultiTenant;
using Finbuckle.MultiTenant.AspNetCore.Extensions;
using Finbuckle.MultiTenant.EntityFrameworkCore.Extensions;
using Finbuckle.MultiTenant.Extensions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Commerce.Application.Abstracts;
using Commerce.Domain.Tenants;
using Commerce.Infrastructure.Persistence.Seed;

namespace Commerce.Infrastructure.Persistence;

public static class DependencyInjection
{
    public static bool IsSqlServer(IConfiguration configuration)
        => string.Equals(configuration["Database:Provider"], "SqlServer", StringComparison.OrdinalIgnoreCase);

    public static IServiceCollection AddPersistence(
        this IServiceCollection services,
        IConfiguration configuration,
        bool configureAspNetStrategies = false)
    {
        var sqlServer = IsSqlServer(configuration);
        var connectionString = configuration.GetConnectionString("Commerce");
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException(
                "Connection string 'Commerce' is required. Set ConnectionStrings__Commerce.");
        }

        var migrationsAssembly = typeof(CommerceDbContextBase).Assembly.FullName;

        if (sqlServer)
        {
            services.AddDbContext<SqlServerCommerceDbContext>(options =>
                options.UseSqlServer(connectionString, sql =>
                {
                    sql.MigrationsAssembly(migrationsAssembly);
                    sql.MigrationsHistoryTable("__EFMigrationsHistory_App");
                }));
            services.AddScoped<ICommerceDbContext>(sp => sp.GetRequiredService<SqlServerCommerceDbContext>());

            services.AddDbContext<SqlServerCommerceTenantStoreDbContext>(options =>
                options.UseSqlServer(connectionString, sql =>
                {
                    sql.MigrationsAssembly(migrationsAssembly);
                    sql.MigrationsHistoryTable("__EFMigrationsHistory_TenantStore");
                }));
            services.AddScoped<ICommerceTenantDatabase>(sp => sp.GetRequiredService<SqlServerCommerceTenantStoreDbContext>());
        }
        else
        {
            services.AddDbContext<PostgresCommerceDbContext>(options =>
                options.UseNpgsql(connectionString, npgsql =>
                {
                    npgsql.MigrationsAssembly(migrationsAssembly);
                    npgsql.MigrationsHistoryTable("__EFMigrationsHistory_App");
                }));
            services.AddScoped<ICommerceDbContext>(sp => sp.GetRequiredService<PostgresCommerceDbContext>());

            services.AddDbContext<PostgresCommerceTenantStoreDbContext>(options =>
                options.UseNpgsql(connectionString, npgsql =>
                {
                    npgsql.MigrationsAssembly(migrationsAssembly);
                    npgsql.MigrationsHistoryTable("__EFMigrationsHistory_TenantStore");
                }));
            services.AddScoped<ICommerceTenantDatabase>(sp => sp.GetRequiredService<PostgresCommerceTenantStoreDbContext>());
        }

        services.AddScoped<ITenantStore, EfTenantStore>();
        services.AddSingleton<ITenantProvisioner, TenantProvisioner>();

        var multiTenant = services.AddMultiTenant<CommerceTenantInfo>();
        if (configureAspNetStrategies)
        {
            multiTenant
                .WithRouteStrategy()
                .WithHeaderStrategy("X-Tenant");
        }

        if (sqlServer)
        {
            multiTenant.WithEFCoreStore<SqlServerCommerceTenantStoreDbContext, CommerceTenantInfo>();
        }
        else
        {
            multiTenant.WithEFCoreStore<PostgresCommerceTenantStoreDbContext, CommerceTenantInfo>();
        }

        return services;
    }

    public static async Task MigrateAndSeedAsync(this IServiceProvider services, CancellationToken cancellationToken = default)
    {
        using var scope = services.CreateScope();
        var tenantStore = scope.ServiceProvider.GetRequiredService<ICommerceTenantDatabase>();
        await tenantStore.MigrateAsync(cancellationToken);
        await DefaultTenantSeeder.EnsureDefaultTenantsAsync(services, cancellationToken);

        var firstTenant = await tenantStore.TenantInfo.FirstAsync(cancellationToken);
        TenantBootstrap.SetCurrentTenant(scope.ServiceProvider, firstTenant);

        var db = (DbContext)scope.ServiceProvider.GetRequiredService<ICommerceDbContext>();
        await db.Database.MigrateAsync(cancellationToken);

        var provisioner = scope.ServiceProvider.GetRequiredService<ITenantProvisioner>();
        var allTenants = await tenantStore.TenantInfo.AsNoTracking().ToListAsync(cancellationToken);
        foreach (var tenant in allTenants)
        {
            await provisioner.EnsureDefaultsAsync(tenant, cancellationToken);
        }
    }
}
