using Finbuckle.MultiTenant.Abstractions;
using Microsoft.Extensions.Logging;
using Commerce.Application.Abstracts;
using Commerce.Application.Common.Abstracts;
using Commerce.Application.Features.DataIngestion;
using Commerce.Domain.Tenants;

namespace Commerce.Application.Features.DataIngestion.GetIngestedCatalogItem;

public sealed class GetIngestedCatalogItemHandler(
    ICommerceDbContext db,
    IGravityStoreDataClient gravity,
    IMultiTenantContextAccessor<CommerceTenantInfo> tenantAccessor,
    ILogger<GetIngestedCatalogItemHandler> logger)
    : IQueryHandler<GetIngestedCatalogItemQuery, GetIngestedCatalogItemResult?>
{
    public async Task<GetIngestedCatalogItemResult?> Handle(
        GetIngestedCatalogItemQuery request,
        CancellationToken cancellationToken)
    {
        _ = tenantAccessor.MultiTenantContext?.TenantInfo
            ?? throw new InvalidOperationException("Tenant is required.");

        var (_, credentials) = await GravityIntegrationAccess.LoadAsync(
            db,
            request.IntegrationId,
            logger,
            cancellationToken);

        logger.LogInformation(
            "Catalog live detail. IntegrationId={IntegrationId} ProductId={ProductId}",
            request.IntegrationId,
            request.Id);

        var product = await gravity.GetProductByIdAsync(credentials, request.Id, cancellationToken);
        if (product is null || string.IsNullOrWhiteSpace(product.ProductId))
            return null;

        return new GetIngestedCatalogItemResult(
            product.ProductId,
            product.Name,
            product.ProductStatusName,
            product.UpdatedOn,
            DateTimeOffset.UtcNow,
            product.PayloadJson);
    }
}
