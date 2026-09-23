using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Commerce.Domain.Policies;

namespace Commerce.Infrastructure.Persistence.Configurations;

public sealed class PolicyEvalAnswerConfiguration : IEntityTypeConfiguration<PolicyEvalAnswer>
{
    public void Configure(EntityTypeBuilder<PolicyEvalAnswer> builder)
    {
        builder.ToTable("PolicyEvalAnswers");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.QuestionId).HasMaxLength(64).IsRequired();
        builder.Property(x => x.Question).HasMaxLength(500).IsRequired();
        builder.Property(x => x.ToolResultJson).IsRequired();
        builder.Property(x => x.Answer).IsRequired();
        builder.Property(x => x.JudgeReason).HasMaxLength(500);
        builder.HasIndex(x => x.RunId);
    }
}
