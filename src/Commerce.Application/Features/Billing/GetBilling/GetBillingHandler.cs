using Microsoft.EntityFrameworkCore;
using Commerce.Application.Abstracts;
using Commerce.Application.Common.Abstracts;

namespace Commerce.Application.Features.Billing.GetBilling;

public sealed class GetBillingHandler(ICommerceDbContext db)
    : IQueryHandler<GetBillingQuery, GetBillingResult?>
{
    public async Task<GetBillingResult?> Handle(GetBillingQuery request, CancellationToken cancellationToken)
    {
        return await db.TenantBillings
            .Select(b => new GetBillingResult(
                b.Id,
                b.PlanCode,
                b.Status.ToString(),
                b.StripeCustomerId,
                b.TrialEndsAt,
                b.UpdatedAt))
            .FirstOrDefaultAsync(cancellationToken);
    }
}
