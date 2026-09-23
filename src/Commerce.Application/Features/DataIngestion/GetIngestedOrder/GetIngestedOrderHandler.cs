using Finbuckle.MultiTenant.Abstractions;
using Microsoft.Extensions.Logging;
using Commerce.Application.Abstracts;
using Commerce.Application.Common.Abstracts;
using Commerce.Application.Features.DataIngestion;
using Commerce.Domain.Tenants;

namespace Commerce.Application.Features.DataIngestion.GetIngestedOrder;

public sealed class GetIngestedOrderHandler(
    ICommerceDbContext db,
    IGravityStoreDataClient gravity,
    IMultiTenantContextAccessor<CommerceTenantInfo> tenantAccessor,
    ILogger<GetIngestedOrderHandler> logger)
    : IQueryHandler<GetIngestedOrderQuery, GetIngestedOrderResult?>
{
    public async Task<GetIngestedOrderResult?> Handle(GetIngestedOrderQuery request, CancellationToken cancellationToken)
    {
        _ = tenantAccessor.MultiTenantContext?.TenantInfo
            ?? throw new InvalidOperationException("Tenant is required.");

        var (_, credentials) = await GravityIntegrationAccess.LoadAsync(
            db,
            request.IntegrationId,
            logger,
            cancellationToken);

        logger.LogInformation(
            "Order live detail. IntegrationId={IntegrationId} OrderId={OrderId}",
            request.IntegrationId,
            request.Id);

        var order = await gravity.GetOrderByIdAsync(credentials, request.Id, cancellationToken);
        if (order is null || string.IsNullOrWhiteSpace(order.SaleOrderId))
            return null;

        return new GetIngestedOrderResult(
            order.SaleOrderId,
            order.Name,
            order.Status,
            order.UpdatedOn,
            DateTimeOffset.UtcNow,
            order.PayloadJson);
    }
}
