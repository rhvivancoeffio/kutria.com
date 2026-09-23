using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Commerce.Domain.Auth;

namespace Commerce.Infrastructure.Persistence.Configurations;

public sealed class TenantOwnerConfiguration : IEntityTypeConfiguration<TenantOwner>
{
    public void Configure(EntityTypeBuilder<TenantOwner> builder)
    {
        builder.ToTable("TenantOwners");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.TenantId).HasMaxLength(64).IsRequired();
        builder.Property(x => x.Email).HasMaxLength(256).IsRequired();
        builder.Property(x => x.DisplayName).HasMaxLength(200).IsRequired();
        builder.Property(x => x.PasswordHash).HasMaxLength(500).IsRequired();
        builder.HasIndex(x => x.TenantId).IsUnique();
        builder.HasIndex(x => x.Email).IsUnique();
    }
}
