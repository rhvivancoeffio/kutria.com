using Finbuckle.MultiTenant.EntityFrameworkCore.Extensions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Commerce.Domain.ApiKeys;

namespace Commerce.Infrastructure.Persistence.Configurations;

public sealed class TenantApiKeyConfiguration : IEntityTypeConfiguration<TenantApiKey>
{
    public void Configure(EntityTypeBuilder<TenantApiKey> builder)
    {
        builder.ToTable("TenantApiKeys");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.TenantId).HasMaxLength(64).IsRequired();
        builder.Property(x => x.Name).HasMaxLength(200).IsRequired();
        builder.Property(x => x.KeyPrefix).HasMaxLength(32).IsRequired();
        builder.Property(x => x.KeyHash).HasMaxLength(128).IsRequired();
        builder.HasIndex(x => x.KeyHash).IsUnique();
        builder.HasIndex(x => new { x.TenantId, x.WorkspaceId });
        builder.IsMultiTenant();
    }
}
