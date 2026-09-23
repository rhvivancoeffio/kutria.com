using Microsoft.EntityFrameworkCore;
using Commerce.Domain.Agents;
using Commerce.Domain.ApiKeys;
using Commerce.Domain.Brains;
using Commerce.Domain.Auth;
using Commerce.Domain.Billing;
using Commerce.Domain.Integrations;
using Commerce.Domain.Mcp;
using Commerce.Domain.Policies;
using Commerce.Domain.Workspaces;

namespace Commerce.Application.Abstracts;

public interface ICommerceDbContext
{
    DbSet<Workspace> Workspaces { get; }
    DbSet<TenantBilling> TenantBillings { get; }
    DbSet<BillingInvoice> BillingInvoices { get; }
    DbSet<Integration> Integrations { get; }
    DbSet<TenantAgentDefinition> TenantAgentDefinitions { get; }
    DbSet<Proposal> Proposals { get; }
    DbSet<BrainIngestJob> BrainIngestJobs { get; }
    DbSet<TenantOwner> TenantOwners { get; }
    DbSet<PolicyEvalItem> PolicyEvalItems { get; }
    DbSet<PolicyEvalRun> PolicyEvalRuns { get; }
    DbSet<PolicyEvalAnswer> PolicyEvalAnswers { get; }
    DbSet<TenantApiKey> TenantApiKeys { get; }
    DbSet<McpOAuthClient> McpOAuthClients { get; }
    DbSet<McpOAuthAuthorizationCode> McpOAuthAuthorizationCodes { get; }
    DbSet<McpOAuthRefreshToken> McpOAuthRefreshTokens { get; }
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
