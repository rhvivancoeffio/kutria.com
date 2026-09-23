using Finbuckle.MultiTenant.Abstractions;
using Microsoft.Extensions.Logging;
using Commerce.Application.Abstracts;
using Commerce.Application.Common.Abstracts;
using Commerce.Application.Features.DataIngestion;
using Commerce.Domain.Tenants;

namespace Commerce.Application.Features.DataIngestion.ListIngestionsOrders;

public sealed class ListIngestionsOrdersHandler(
    ICommerceDbContext db,
    IDataIngestTableStore tables,
    IMultiTenantContextAccessor<CommerceTenantInfo> tenantAccessor,
    ILogger<ListIngestionsOrdersHandler> logger)
    : IQueryHandler<ListIngestionsOrdersQuery, ListIngestionsOrdersResult>
{
    public async Task<ListIngestionsOrdersResult> Handle(
        ListIngestionsOrdersQuery request,
        CancellationToken cancellationToken)
    {
        var tenant = tenantAccessor.MultiTenantContext?.TenantInfo
            ?? throw new InvalidOperationException("Tenant is required.");

        await GravityIntegrationAccess.EnsureExistsAsync(db, request.IntegrationId, cancellationToken);

        var pageSize = Math.Clamp(request.PageSize, 1, 100);
        var partition = DataIngestPartition.Key(tenant.Id!, request.IntegrationId);

        logger.LogInformation(
            "Ingestions orders list. IntegrationId={IntegrationId} PageSize={PageSize} HasToken={HasToken}",
            request.IntegrationId,
            pageSize,
            !string.IsNullOrWhiteSpace(request.NextToken));

        var batch = await tables.ListOrdersAsync(partition, pageSize, request.NextToken, cancellationToken);

        var items = batch.Items
            .Select(x =>
            {
                var summary = GravityPayloadMapper.MapOrder(x.RowKey, x.Name, x.ProductStatusName, x.PayloadJson);
                return new ListIngestionsOrderItem(
                    summary.Id,
                    summary.Name,
                    summary.Status,
                    x.UpdatedOn,
                    x.SyncedAt,
                    summary.OrderNumber,
                    summary.OrderDate,
                    summary.DeliveryDate,
                    summary.ClientName,
                    summary.ClientSecondary,
                    summary.SellerName,
                    summary.ItemCount,
                    summary.Total,
                    summary.CurrencySymbol,
                    summary.ProviderNames);
            })
            .ToList();

        return new ListIngestionsOrdersResult(items, batch.PageSize, batch.NextToken, batch.HasMore);
    }
}
