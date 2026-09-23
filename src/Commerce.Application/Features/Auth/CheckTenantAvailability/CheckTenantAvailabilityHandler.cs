using Commerce.Application.Abstracts;
using Commerce.Application.Common.Abstracts;

namespace Commerce.Application.Features.Auth.CheckTenantAvailability;

public sealed class CheckTenantAvailabilityHandler(ITenantStore tenants)
    : IQueryHandler<CheckTenantAvailabilityQuery, CheckTenantAvailabilityResult>
{
    public async Task<CheckTenantAvailabilityResult> Handle(
        CheckTenantAvailabilityQuery request,
        CancellationToken cancellationToken)
    {
        var identifier = TenantIdentifierRules.Slugify(request.Identifier);
        if (!TenantIdentifierRules.IsFormatValid(identifier))
        {
            return new CheckTenantAvailabilityResult(identifier, false, "invalid");
        }

        if (TenantIdentifierRules.IsReserved(identifier))
        {
            return new CheckTenantAvailabilityResult(identifier, false, "reserved");
        }

        var existing = await tenants.GetByIdentifierAsync(identifier, cancellationToken);
        if (existing is not null)
        {
            return new CheckTenantAvailabilityResult(identifier, false, "taken");
        }

        return new CheckTenantAvailabilityResult(identifier, true, null);
    }
}
