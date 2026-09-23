using Finbuckle.MultiTenant.EntityFrameworkCore.Extensions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Commerce.Domain.Billing;

namespace Commerce.Infrastructure.Persistence.Configurations;

public sealed class TenantBillingConfiguration : IEntityTypeConfiguration<TenantBilling>
{
    public void Configure(EntityTypeBuilder<TenantBilling> builder)
    {
        builder.ToTable("TenantBillings");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.TenantId).HasMaxLength(64).IsRequired();
        builder.Property(x => x.PlanCode).HasMaxLength(64).IsRequired();
        builder.Property(x => x.Status).HasConversion<string>().HasMaxLength(32);
        builder.Property(x => x.StripeCustomerId).HasMaxLength(128);
        builder.Property(x => x.StripeSubscriptionId).HasMaxLength(128);
        builder.HasIndex(x => x.TenantId).IsUnique();
        builder.IsMultiTenant();
    }
}
