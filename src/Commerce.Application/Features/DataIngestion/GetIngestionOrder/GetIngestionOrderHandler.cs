using Finbuckle.MultiTenant.Abstractions;
using Commerce.Application.Abstracts;
using Commerce.Application.Common.Abstracts;
using Commerce.Application.Features.DataIngestion;
using Commerce.Domain.Tenants;

namespace Commerce.Application.Features.DataIngestion.GetIngestionOrder;

public sealed class GetIngestionOrderHandler(
    ICommerceDbContext db,
    IDataIngestTableStore tables,
    IMultiTenantContextAccessor<CommerceTenantInfo> tenantAccessor)
    : IQueryHandler<GetIngestionOrderQuery, GetIngestionOrderResult?>
{
    public async Task<GetIngestionOrderResult?> Handle(
        GetIngestionOrderQuery request,
        CancellationToken cancellationToken)
    {
        var tenant = tenantAccessor.MultiTenantContext?.TenantInfo
            ?? throw new InvalidOperationException("Tenant is required.");

        await GravityIntegrationAccess.EnsureExistsAsync(db, request.IntegrationId, cancellationToken);

        var partition = DataIngestPartition.Key(tenant.Id!, request.IntegrationId);
        var row = await tables.GetOrderAsync(partition, request.Id, cancellationToken);
        if (row is null)
            return null;

        return new GetIngestionOrderResult(
            row.RowKey,
            row.Name,
            row.ProductStatusName,
            row.UpdatedOn,
            row.SyncedAt,
            row.PayloadJson);
    }
}
