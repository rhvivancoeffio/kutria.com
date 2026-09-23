using Finbuckle.MultiTenant.Abstractions;
using Microsoft.Extensions.Logging;
using Commerce.Application.Abstracts;
using Commerce.Application.Common.Abstracts;
using Commerce.Application.Features.DataIngestion;
using Commerce.Domain.Tenants;

namespace Commerce.Application.Features.DataIngestion.ListIngestionsCatalog;

public sealed class ListIngestionsCatalogHandler(
    ICommerceDbContext db,
    IDataIngestTableStore tables,
    IMultiTenantContextAccessor<CommerceTenantInfo> tenantAccessor,
    ILogger<ListIngestionsCatalogHandler> logger)
    : IQueryHandler<ListIngestionsCatalogQuery, ListIngestionsCatalogResult>
{
    public async Task<ListIngestionsCatalogResult> Handle(
        ListIngestionsCatalogQuery request,
        CancellationToken cancellationToken)
    {
        var tenant = tenantAccessor.MultiTenantContext?.TenantInfo
            ?? throw new InvalidOperationException("Tenant is required.");

        await GravityIntegrationAccess.EnsureExistsAsync(db, request.IntegrationId, cancellationToken);

        var pageSize = Math.Clamp(request.PageSize, 1, 100);
        var partition = DataIngestPartition.Key(tenant.Id!, request.IntegrationId);

        logger.LogInformation(
            "Ingestions catalog list. IntegrationId={IntegrationId} PageSize={PageSize} HasToken={HasToken}",
            request.IntegrationId,
            pageSize,
            !string.IsNullOrWhiteSpace(request.NextToken));

        var batch = await tables.ListCatalogAsync(partition, pageSize, request.NextToken, cancellationToken);

        var items = batch.Items
            .Select(x =>
            {
                var summary = GravityPayloadMapper.MapCatalog(x.RowKey, x.Name, x.ProductStatusName, x.PayloadJson);
                return new ListIngestionsCatalogItem(
                    summary.Id,
                    summary.Name,
                    summary.Status,
                    x.UpdatedOn,
                    x.SyncedAt,
                    summary.ImageUrl,
                    summary.SellerName,
                    summary.BrandName,
                    summary.CategoryPath,
                    summary.MarketplaceId,
                    summary.Stock,
                    summary.BasePrice,
                    summary.SpecialPrice,
                    summary.CurrencySymbol);
            })
            .ToList();

        return new ListIngestionsCatalogResult(items, batch.PageSize, batch.NextToken, batch.HasMore);
    }
}
