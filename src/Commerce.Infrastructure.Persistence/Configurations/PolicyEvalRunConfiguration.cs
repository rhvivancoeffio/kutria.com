using Finbuckle.MultiTenant.EntityFrameworkCore.Extensions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Commerce.Domain.Policies;

namespace Commerce.Infrastructure.Persistence.Configurations;

public sealed class PolicyEvalRunConfiguration : IEntityTypeConfiguration<PolicyEvalRun>
{
    public void Configure(EntityTypeBuilder<PolicyEvalRun> builder)
    {
        builder.ToTable("PolicyEvalRuns");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.TenantId).HasMaxLength(64).IsRequired();
        builder.Property(x => x.TemplateVersion).HasMaxLength(32).IsRequired();
        builder.Property(x => x.Status).HasMaxLength(32).IsRequired();
        builder.HasIndex(x => new { x.TenantId, x.WorkspaceId, x.JobId, x.CreatedAt });
        builder.HasMany(x => x.Answers).WithOne(x => x.Run).HasForeignKey(x => x.RunId);
        builder.IsMultiTenant();
    }
}
