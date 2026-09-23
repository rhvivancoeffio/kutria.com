using Finbuckle.MultiTenant.EntityFrameworkCore.Extensions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Commerce.Domain.Brains;

namespace Commerce.Infrastructure.Persistence.Configurations;

public sealed class BrainIngestJobConfiguration : IEntityTypeConfiguration<BrainIngestJob>
{
    public void Configure(EntityTypeBuilder<BrainIngestJob> builder)
    {
        builder.ToTable("BrainIngestJobs");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.TenantId).HasMaxLength(64).IsRequired();
        builder.Property(x => x.BrainKey).HasMaxLength(64).IsRequired();
        builder.Property(x => x.FileName).HasMaxLength(260).IsRequired();
        builder.Property(x => x.StorageKey).HasMaxLength(500).IsRequired();
        builder.Property(x => x.Status).HasMaxLength(32).IsRequired();
        builder.Property(x => x.Summary).HasMaxLength(200);
        builder.Property(x => x.Error).HasMaxLength(2000);
        builder.Property(x => x.MetadataJson).IsRequired();
        builder.Property(x => x.EvaluationStatus).HasMaxLength(32).IsRequired();
        builder.HasIndex(x => new { x.TenantId, x.WorkspaceId, x.BrainKey, x.Status });
        builder.IsMultiTenant();
    }
}
