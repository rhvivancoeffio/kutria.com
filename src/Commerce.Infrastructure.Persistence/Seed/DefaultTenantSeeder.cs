using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Commerce.Domain.Tenants;

namespace Commerce.Infrastructure.Persistence.Seed;

public static class DefaultTenantSeeder
{
    private static readonly (string Id, string Identifier, string Name)[] RequiredTenants =
    [
        ("tenant1", "tenant1", "Tenant 1"),
        ("tenant2", "tenant2", "Tenant 2"),
        ("tenant3", "tenant3", "Tenant 3")
    ];

    public static async Task EnsureDefaultTenantsAsync(IServiceProvider services, CancellationToken cancellationToken = default)
    {
        using var scope = services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ICommerceTenantDatabase>();

        foreach (var required in RequiredTenants)
        {
            var exists = await db.TenantInfo.AnyAsync(t => t.Identifier == required.Identifier, cancellationToken);
            if (exists)
            {
                continue;
            }

            db.TenantInfo.Add(new CommerceTenantInfo
            {
                Id = required.Id,
                Identifier = required.Identifier,
                Name = required.Name
            });
        }

        try
        {
            await db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException)
        {
            ((DbContext)db).ChangeTracker.Clear();
        }
    }
}
