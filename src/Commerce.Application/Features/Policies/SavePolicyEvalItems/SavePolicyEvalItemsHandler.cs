using Finbuckle.MultiTenant.Abstractions;
using Microsoft.EntityFrameworkCore;
using Commerce.Application.Abstracts;
using Commerce.Application.Common;
using Commerce.Application.Common.Abstracts;
using Commerce.Domain.Policies;
using Commerce.Domain.Tenants;

namespace Commerce.Application.Features.Policies.SavePolicyEvalItems;

public sealed class SavePolicyEvalItemsHandler(
    ICommerceDbContext db,
    IPolicyEvalCatalog catalog,
    IMultiTenantContextAccessor<CommerceTenantInfo> tenantAccessor,
    IWorkspaceContext workspaceContext)
    : ICommandHandler<SavePolicyEvalItemsCommand, SavePolicyEvalItemsResult>
{
    public async Task<SavePolicyEvalItemsResult> Handle(SavePolicyEvalItemsCommand request, CancellationToken cancellationToken)
    {
        _ = tenantAccessor.MultiTenantContext?.TenantInfo
            ?? throw new InvalidOperationException("Tenant is required.");
        var workspaceId = await WorkspaceRequirement.RequireWorkspaceIdAsync(
            workspaceContext, db, cancellationToken);
        var baseItems = catalog.LoadBase().ToDictionary(item => item.Id, StringComparer.OrdinalIgnoreCase);
        var existing = await db.PolicyEvalItems
            .Where(x => x.WorkspaceId == workspaceId)
            .ToListAsync(cancellationToken);
        db.PolicyEvalItems.RemoveRange(existing);

        var sort = 0;
        foreach (var item in request.Items)
        {
            var question = item.Question.Trim();
            if (!string.IsNullOrWhiteSpace(item.BaseItemId) && baseItems.TryGetValue(item.BaseItemId, out var golden))
            {
                if (!item.IsDisabled && string.Equals(question, golden.Question, StringComparison.Ordinal))
                {
                    continue;
                }

                db.PolicyEvalItems.Add(new PolicyEvalItem
                {
                    WorkspaceId = workspaceId,
                    BaseItemId = golden.Id,
                    Type = golden.Type,
                    Question = question,
                    IsDisabled = item.IsDisabled,
                    SortOrder = sort++
                });
                continue;
            }

            if (string.IsNullOrWhiteSpace(item.BaseItemId) && !string.IsNullOrWhiteSpace(item.Type))
            {
                db.PolicyEvalItems.Add(new PolicyEvalItem
                {
                    WorkspaceId = workspaceId,
                    Type = item.Type.Trim().ToLowerInvariant(),
                    Question = question,
                    IsDisabled = item.IsDisabled,
                    SortOrder = sort++
                });
            }
        }

        await db.SaveChangesAsync(cancellationToken);
        return new SavePolicyEvalItemsResult(sort);
    }
}
