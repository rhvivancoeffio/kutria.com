using Finbuckle.MultiTenant.Abstractions;
using Microsoft.EntityFrameworkCore;
using Commerce.Application.Abstracts;
using Commerce.Application.Common.Abstracts;
using Commerce.Domain.Billing;
using Commerce.Domain.Tenants;

namespace Commerce.Application.Features.Billing.UpdateBillingPlan;

public sealed class UpdateBillingPlanHandler(
    ICommerceDbContext db,
    IMultiTenantContextAccessor<CommerceTenantInfo> tenantAccessor)
    : ICommandHandler<UpdateBillingPlanCommand, UpdateBillingPlanResult>
{
    public async Task<UpdateBillingPlanResult> Handle(
        UpdateBillingPlanCommand request,
        CancellationToken cancellationToken)
    {
        var tenant = tenantAccessor.MultiTenantContext?.TenantInfo
            ?? throw new InvalidOperationException("Tenant is required.");

        var billing = await db.TenantBillings.FirstOrDefaultAsync(cancellationToken);
        if (billing is null)
        {
            billing = new TenantBilling
            {
                TenantId = tenant.Id!,
                PlanCode = "free",
                Status = BillingStatus.Trialing,
                TrialEndsAt = DateTimeOffset.UtcNow.AddDays(14)
            };
            db.TenantBillings.Add(billing);
        }

        billing.PlanCode = request.PlanCode.Trim().ToLowerInvariant();
        billing.Status = billing.PlanCode == "free" ? BillingStatus.Trialing : BillingStatus.Active;
        billing.UpdatedAt = DateTimeOffset.UtcNow;

        await db.SaveChangesAsync(cancellationToken);

        return new UpdateBillingPlanResult(
            billing.Id,
            billing.PlanCode,
            billing.Status.ToString(),
            billing.UpdatedAt);
    }
}
