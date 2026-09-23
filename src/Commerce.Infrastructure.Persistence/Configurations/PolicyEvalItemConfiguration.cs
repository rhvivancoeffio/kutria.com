using Finbuckle.MultiTenant.EntityFrameworkCore.Extensions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Commerce.Domain.Policies;

namespace Commerce.Infrastructure.Persistence.Configurations;

public sealed class PolicyEvalItemConfiguration : IEntityTypeConfiguration<PolicyEvalItem>
{
    public void Configure(EntityTypeBuilder<PolicyEvalItem> builder)
    {
        builder.ToTable("PolicyEvalItems");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.TenantId).HasMaxLength(64).IsRequired();
        builder.Property(x => x.BaseItemId).HasMaxLength(64);
        builder.Property(x => x.Type).HasMaxLength(32).IsRequired();
        builder.Property(x => x.Question).HasMaxLength(500).IsRequired();
        builder.HasIndex(x => new { x.TenantId, x.WorkspaceId, x.BaseItemId });
        builder.IsMultiTenant();
    }
}
