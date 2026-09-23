namespace Commerce.Application.Features.Auth.CheckTenantAvailability;

public sealed record CheckTenantAvailabilityResult(string Identifier, bool Available, string? Reason);
