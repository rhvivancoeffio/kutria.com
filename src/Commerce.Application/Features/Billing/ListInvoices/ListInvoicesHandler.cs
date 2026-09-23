using Microsoft.EntityFrameworkCore;
using Commerce.Application.Abstracts;
using Commerce.Application.Common.Abstracts;

namespace Commerce.Application.Features.Billing.ListInvoices;

public sealed class ListInvoicesHandler(ICommerceDbContext db)
    : IQueryHandler<ListInvoicesQuery, ListInvoicesResult>
{
    public async Task<ListInvoicesResult> Handle(ListInvoicesQuery request, CancellationToken cancellationToken)
    {
        var items = await db.BillingInvoices
            .OrderByDescending(i => i.CreatedAt)
            .Select(i => new InvoiceListItem(
                i.Id,
                i.InvoiceNumber,
                i.Amount,
                i.Currency,
                i.Status,
                i.PeriodStart,
                i.PeriodEnd,
                i.CreatedAt))
            .ToListAsync(cancellationToken);

        return new ListInvoicesResult(items);
    }
}
