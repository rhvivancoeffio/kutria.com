namespace Commerce.Application.Features.Billing.ListInvoices;

public sealed record ListInvoicesResult(IReadOnlyList<InvoiceListItem> Items);
