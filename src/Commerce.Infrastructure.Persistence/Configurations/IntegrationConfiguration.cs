using Finbuckle.MultiTenant.EntityFrameworkCore.Extensions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Commerce.Domain.Integrations;

namespace Commerce.Infrastructure.Persistence.Configurations;

public sealed class IntegrationConfiguration : IEntityTypeConfiguration<Integration>
{
    public void Configure(EntityTypeBuilder<Integration> builder)
    {
        builder.ToTable("Integrations");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.TenantId).HasMaxLength(64).IsRequired();
        builder.Property(x => x.Provider).HasMaxLength(64).IsRequired();
        builder.Property(x => x.Name).HasMaxLength(200).IsRequired();
        builder.Property(x => x.SettingsJson).HasColumnType("text").IsRequired();
        builder.HasIndex(x => x.TenantId);
        builder.HasIndex(x => new { x.TenantId, x.WorkspaceId, x.Provider, x.Name });
        builder.IsMultiTenant();
    }
}
