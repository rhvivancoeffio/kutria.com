using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Commerce.Domain.Mcp;

namespace Commerce.Infrastructure.Persistence.Configurations;

public sealed class McpOAuthClientConfiguration : IEntityTypeConfiguration<McpOAuthClient>
{
    public void Configure(EntityTypeBuilder<McpOAuthClient> builder)
    {
        builder.ToTable("McpOAuthClients");
        builder.HasKey(x => x.ClientId);
        builder.Property(x => x.ClientId).HasMaxLength(64);
        builder.Property(x => x.RedirectUrisJson).IsRequired();
        builder.Property(x => x.ClientName).HasMaxLength(200);
    }
}

public sealed class McpOAuthAuthorizationCodeConfiguration : IEntityTypeConfiguration<McpOAuthAuthorizationCode>
{
    public void Configure(EntityTypeBuilder<McpOAuthAuthorizationCode> builder)
    {
        builder.ToTable("McpOAuthAuthorizationCodes");
        builder.HasKey(x => x.Code);
        builder.Property(x => x.Code).HasMaxLength(128);
        builder.Property(x => x.ClientId).HasMaxLength(64).IsRequired();
        builder.Property(x => x.RedirectUri).HasMaxLength(2000).IsRequired();
        builder.Property(x => x.CodeChallenge).HasMaxLength(256).IsRequired();
        builder.Property(x => x.CodeChallengeMethod).HasMaxLength(16).IsRequired();
        builder.Property(x => x.UserId).HasMaxLength(64).IsRequired();
        builder.Property(x => x.TenantId).HasMaxLength(64).IsRequired();
        builder.Property(x => x.Resource).HasMaxLength(500);
        builder.Property(x => x.Scope).HasMaxLength(500);
        builder.HasIndex(x => new { x.Code, x.ClientId });
    }
}

public sealed class McpOAuthRefreshTokenConfiguration : IEntityTypeConfiguration<McpOAuthRefreshToken>
{
    public void Configure(EntityTypeBuilder<McpOAuthRefreshToken> builder)
    {
        builder.ToTable("McpOAuthRefreshTokens");
        builder.HasKey(x => x.Token);
        builder.Property(x => x.Token).HasMaxLength(256);
        builder.Property(x => x.ClientId).HasMaxLength(64).IsRequired();
        builder.Property(x => x.UserId).HasMaxLength(64).IsRequired();
        builder.Property(x => x.TenantId).HasMaxLength(64).IsRequired();
        builder.Property(x => x.Resource).HasMaxLength(500);
        builder.Property(x => x.Scope).HasMaxLength(500);
        builder.HasIndex(x => new { x.Token, x.ClientId });
    }
}
