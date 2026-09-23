using Finbuckle.MultiTenant.Abstractions;
using Microsoft.Extensions.Logging;
using Commerce.Application.Abstracts;
using Commerce.Application.Common.Abstracts;
using Commerce.Application.Features.DataIngestion;
using Commerce.Domain.Tenants;

namespace Commerce.Application.Features.DataIngestion.ListIngestedCatalog;

public sealed class ListIngestedCatalogHandler(
    ICommerceDbContext db,
    IGravityStoreDataClient gravity,
    IMultiTenantContextAccessor<CommerceTenantInfo> tenantAccessor,
    ILogger<ListIngestedCatalogHandler> logger)
    : IQueryHandler<ListIngestedCatalogQuery, ListIngestedCatalogResult>
{
    public async Task<ListIngestedCatalogResult> Handle(ListIngestedCatalogQuery request, CancellationToken cancellationToken)
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
            "Catalog live fetch. IntegrationId={IntegrationId} Page={Page} PageSize={PageSize}",
            request.IntegrationId,
            page,
            pageSize);

        var batch = await gravity.ListProductsAsync(credentials, page, pageSize, cancellationToken);
        var fetchedAt = DateTimeOffset.UtcNow;

        logger.LogInformation(
            "Catalog live fetch done. IntegrationId={IntegrationId} Results={Results} PageCount={PageCount} RowCount={RowCount}",
            request.IntegrationId,
            batch.Results.Count,
            batch.PageCount,
            batch.RowCount);

        return new ListIngestedCatalogResult(
            batch.Results
                .Where(x => !string.IsNullOrWhiteSpace(x.ProductId))
                .Select(x => new ListIngestedCatalogItem(
                    x.ProductId,
                    x.Name,
                    x.ProductStatusName,
                    x.UpdatedOn,
                    fetchedAt,
                    x.ImageUrl,
                    x.SellerName,
                    x.BrandName,
                    x.CategoryPath,
                    x.MarketplaceId,
                    x.Stock,
                    x.BasePrice,
                    x.SpecialPrice,
                    x.CurrencySymbol))
                .ToList(),
            batch.CurrentPage > 0 ? batch.CurrentPage : page,
            batch.PageSize > 0 ? batch.PageSize : pageSize,
            batch.CurrentPage < batch.PageCount);
    }
}
