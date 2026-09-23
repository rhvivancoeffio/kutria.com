using Finbuckle.MultiTenant.Abstractions;
using Finbuckle.MultiTenant.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Commerce.Application.Abstracts;
using Commerce.Domain.Agents;
using Commerce.Domain.ApiKeys;
using Commerce.Domain.Brains;
using Commerce.Domain.Auth;
using Commerce.Domain.Billing;
using Commerce.Domain.Common;
using Commerce.Domain.Integrations;
using Commerce.Domain.Mcp;
using Commerce.Domain.Policies;
using Commerce.Domain.Workspaces;

namespace Commerce.Infrastructure.Persistence;

public abstract class CommerceDbContextBase : MultiTenantDbContext, ICommerceDbContext
{
    protected CommerceDbContextBase(
        IMultiTenantContextAccessor multiTenantContextAccessor,
        DbContextOptions options)
        : base(multiTenantContextAccessor, options)
    {
    }

    public DbSet<Workspace> Workspaces => Set<Workspace>();
    public DbSet<TenantBilling> TenantBillings => Set<TenantBilling>();
    public DbSet<BillingInvoice> BillingInvoices => Set<BillingInvoice>();
    public DbSet<Integration> Integrations => Set<Integration>();
    public DbSet<TenantAgentDefinition> TenantAgentDefinitions => Set<TenantAgentDefinition>();
    public DbSet<Proposal> Proposals => Set<Proposal>();
    public DbSet<BrainIngestJob> BrainIngestJobs => Set<BrainIngestJob>();
    public DbSet<TenantOwner> TenantOwners => Set<TenantOwner>();
    public DbSet<PolicyEvalItem> PolicyEvalItems => Set<PolicyEvalItem>();
    public DbSet<PolicyEvalRun> PolicyEvalRuns => Set<PolicyEvalRun>();
    public DbSet<PolicyEvalAnswer> PolicyEvalAnswers => Set<PolicyEvalAnswer>();
    public DbSet<TenantApiKey> TenantApiKeys => Set<TenantApiKey>();
    public DbSet<McpOAuthClient> McpOAuthClients => Set<McpOAuthClient>();
    public DbSet<McpOAuthAuthorizationCode> McpOAuthAuthorizationCodes => Set<McpOAuthAuthorizationCode>();
    public DbSet<McpOAuthRefreshToken> McpOAuthRefreshTokens => Set<McpOAuthRefreshToken>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(CommerceDbContextBase).Assembly);
        base.OnModelCreating(modelBuilder);
    }

    public override async Task<int> SaveChangesAsync(
        bool acceptAllChangesOnSuccess,
        CancellationToken cancellationToken = default)
    {
        var tenantId = TenantInfo?.Id;
        if (!string.IsNullOrWhiteSpace(tenantId))
        {
            foreach (var entry in ChangeTracker.Entries<ITenantScoped>())
            {
                if (entry.State == EntityState.Added && string.IsNullOrWhiteSpace(entry.Entity.TenantId))
                {
                    entry.Entity.TenantId = tenantId;
                }
            }
        }

        return await base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
    }
}
