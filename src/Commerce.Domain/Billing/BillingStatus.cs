namespace Commerce.Domain.Billing;

public enum BillingStatus
{
    Trialing = 0,
    Active = 1,
    PastDue = 2,
    Canceled = 3,
    Incomplete = 4
}
