using Finbuckle.MultiTenant.Abstractions;
using Microsoft.EntityFrameworkCore;
using Commerce.Application.Abstracts;
using Commerce.Application.Common;
using Commerce.Application.Common.Abstracts;
using Commerce.Domain.Tenants;

namespace Commerce.Application.Features.Policies.GetPolicyEvalItems;

public sealed class GetPolicyEvalItemsHandler(
    ICommerceDbContext db,
    IPolicyEvalCatalog catalog,
    IMultiTenantContextAccessor<CommerceTenantInfo> tenantAccessor,
    IWorkspaceContext workspaceContext)
    : IQueryHandler<GetPolicyEvalItemsQuery, GetPolicyEvalItemsResult>
{
    public async Task<GetPolicyEvalItemsResult> Handle(GetPolicyEvalItemsQuery request, CancellationToken cancellationToken)
    {
        _ = tenantAccessor.MultiTenantContext?.TenantInfo
            ?? throw new InvalidOperationException("Tenant is required.");
        var workspaceId = await WorkspaceRequirement.RequireWorkspaceIdAsync(
            workspaceContext, db, cancellationToken);
        var set = catalog.LoadBase();
        var version = (await catalog.ResolveAsync(workspaceId: workspaceId, cancellationToken: cancellationToken)).Version;
        var overrides = await db.PolicyEvalItems.AsNoTracking()
            .Where(x => x.WorkspaceId == workspaceId)
            .ToListAsync(cancellationToken);
        var byBase = overrides
            .Where(item => !string.IsNullOrWhiteSpace(item.BaseItemId))
            .GroupBy(item => item.BaseItemId!, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(group => group.Key, group => group.First(), StringComparer.OrdinalIgnoreCase);
        var rows = new List<PolicyEvalItemRow>();
        foreach (var item in set)
        {
            if (byBase.TryGetValue(item.Id, out var custom))
            {
                rows.Add(new PolicyEvalItemRow(item.Id, null, item.Type, custom.Question, custom.IsDisabled, custom.IsDisabled ? "disabled" : "override"));
                continue;
            }

            rows.Add(new PolicyEvalItemRow(item.Id, null, item.Type, item.Question, false, "base"));
        }

        foreach (var extra in overrides.Where(item => string.IsNullOrWhiteSpace(item.BaseItemId)).OrderBy(item => item.SortOrder))
        {
            rows.Add(new PolicyEvalItemRow(null, extra.Id, extra.Type, extra.Question, extra.IsDisabled, "extra"));
        }

        return new GetPolicyEvalItemsResult(version, rows);
    }
}
