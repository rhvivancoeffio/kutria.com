namespace Commerce.Application.Features.Billing.GetBilling;

public sealed record GetBillingResult(
    Guid Id,
    string PlanCode,
    string Status,
    string? StripeCustomerId,
    DateTimeOffset? TrialEndsAt,
    DateTimeOffset UpdatedAt);
