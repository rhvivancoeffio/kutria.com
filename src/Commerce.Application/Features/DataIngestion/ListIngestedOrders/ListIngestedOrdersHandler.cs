using Finbuckle.MultiTenant.Abstractions;
using Microsoft.Extensions.Logging;
using Commerce.Application.Abstracts;
using Commerce.Application.Common.Abstracts;
using Commerce.Application.Features.DataIngestion;
using Commerce.Domain.Tenants;

namespace Commerce.Application.Features.DataIngestion.ListIngestedOrders;

public sealed class ListIngestedOrdersHandler(
    ICommerceDbContext db,
    IGravityStoreDataClient gravity,
    IMultiTenantContextAccessor<CommerceTenantInfo> tenantAccessor,
    ILogger<ListIngestedOrdersHandler> logger)
    : IQueryHandler<ListIngestedOrdersQuery, ListIngestedOrdersResult>
{
    public async Task<ListIngestedOrdersResult> Handle(ListIngestedOrdersQuery request, CancellationToken cancellationToken)
    {
        _ = tenantAccessor.MultiTenantContext?.TenantInfo
            ?? throw new InvalidOperationException("Tenant is required.");

        var page = Math.Max(1, request.Page);
        var pageSize = Math.Clamp(request.PageSize, 1, 100);

        var (_, credentials) = await GravityIntegrationAccess.LoadAsync(
            db,
            request.IntegrationId,
            logger,
            cancellationToken);

        logger.LogInformation(
            "Orders live fetch. IntegrationId={IntegrationId} Page={Page} PageSize={PageSize}",
            request.IntegrationId,
            page,
            pageSize);

        var batch = await gravity.ListOrdersAsync(credentials, page, pageSize, cancellationToken);
        var fetchedAt = DateTimeOffset.UtcNow;

        logger.LogInformation(
            "Orders live fetch done. IntegrationId={IntegrationId} Results={Results} HasMore={HasMore}",
            request.IntegrationId,
            batch.Results.Count,
            batch.HasMore);

        return new ListIngestedOrdersResult(
            batch.Results
                .Where(x => !string.IsNullOrWhiteSpace(x.SaleOrderId))
                .Select(x => new ListIngestedOrderItem(
                    x.SaleOrderId,
                    x.Name,
                    x.Status,
                    x.UpdatedOn,
                    fetchedAt,
                    x.OrderNumber,
                    x.OrderDate,
                    x.DeliveryDate,
                    x.ClientName,
                    x.ClientSecondary,
                    x.SellerName,
                    x.ItemCount,
                    x.Total,
                    x.CurrencySymbol,
                    x.ProviderNames))
                .ToList(),
            batch.CurrentPage > 0 ? batch.CurrentPage : page,
            batch.PageSize > 0 ? batch.PageSize : pageSize,
            batch.HasMore);
    }
}
