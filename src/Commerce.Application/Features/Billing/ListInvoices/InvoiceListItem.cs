namespace Commerce.Application.Features.Billing.ListInvoices;

public sealed record InvoiceListItem(
    Guid Id,
    string InvoiceNumber,
    decimal Amount,
    string Currency,
    string Status,
    DateTimeOffset PeriodStart,
    DateTimeOffset PeriodEnd,
    DateTimeOffset CreatedAt);
