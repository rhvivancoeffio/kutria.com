using Commerce.Domain.Common;

namespace Commerce.Domain.Billing;

public class BillingInvoice : BaseEntity, ITenantScoped
{
    public string TenantId { get; set; } = string.Empty;
    public string InvoiceNumber { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string Currency { get; set; } = "USD";
    public string Status { get; set; } = "open";
    public DateTimeOffset PeriodStart { get; set; }
    public DateTimeOffset PeriodEnd { get; set; }
    public string? StripeInvoiceId { get; set; }
}
