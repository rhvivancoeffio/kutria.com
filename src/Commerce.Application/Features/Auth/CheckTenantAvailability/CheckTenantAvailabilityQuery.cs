using Commerce.Application.Common.Abstracts;

namespace Commerce.Application.Features.Auth.CheckTenantAvailability;

public sealed record CheckTenantAvailabilityQuery(string Identifier) : IQuery<CheckTenantAvailabilityResult>;
