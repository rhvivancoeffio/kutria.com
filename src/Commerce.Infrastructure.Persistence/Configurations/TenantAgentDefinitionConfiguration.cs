using System.Text.Json;
using Finbuckle.MultiTenant.EntityFrameworkCore.Extensions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Commerce.Domain.Agents;

namespace Commerce.Infrastructure.Persistence.Configurations;

public sealed class TenantAgentDefinitionConfiguration : IEntityTypeConfiguration<TenantAgentDefinition>
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    public void Configure(EntityTypeBuilder<TenantAgentDefinition> builder)
    {
        builder.ToTable("TenantAgentDefinitions");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.TenantId).HasMaxLength(64).IsRequired();
        builder.Property(x => x.AgentKey).HasMaxLength(80).IsRequired();
        builder.Property(x => x.Kind).HasMaxLength(32).IsRequired();
        builder.Property(x => x.Name).HasMaxLength(200).IsRequired();
        builder.Property(x => x.Instructions).IsRequired();
        builder.Property(x => x.Model).HasMaxLength(80);
        builder.Property(x => x.Queue).HasMaxLength(80);
        ConfigureStringList(builder.Property(x => x.Tools));
        ConfigureStringList(builder.Property(x => x.Publishes));
        builder.Property(x => x.Yaml).IsRequired();
        builder.Property(x => x.BasedOnHash).HasMaxLength(64).IsRequired();
        builder.HasIndex(x => new { x.TenantId, x.AgentKey }).IsUnique();
        builder.IsMultiTenant();
    }

    private static void ConfigureStringList(PropertyBuilder<List<string>> property)
    {
        property
            .HasConversion(
                value => JsonSerializer.Serialize(value, Json),
                value => string.IsNullOrWhiteSpace(value)
                    ? new List<string>()
                    : JsonSerializer.Deserialize<List<string>>(value, Json) ?? new List<string>())
            .Metadata.SetValueComparer(new ValueComparer<List<string>>(
                (left, right) => (left ?? new List<string>()).SequenceEqual(right ?? new List<string>()),
                value => (value ?? new List<string>()).Aggregate(0, (hash, item) => HashCode.Combine(hash, item.GetHashCode())),
                value => value.ToList()));
    }
}
