using System.Text;
using System.Text.Json;
using Finbuckle.MultiTenant.Abstractions;
using Microsoft.Extensions.Logging;
using Commerce.Application.Abstracts;
using Commerce.Application.Common.Abstracts;
using Commerce.Application.Features.DataIngestion;
using Commerce.Domain.Tenants;

namespace Commerce.Application.Features.DataIngestion.ForceEnqueueCatalogVector;

public sealed class ForceEnqueueCatalogVectorHandler(
    ICommerceDbContext db,
    IDataIngestTableStore tables,
    IGravityStoreDataClient gravity,
    IMessageQueue queue,
    IMultiTenantContextAccessor<CommerceTenantInfo> tenantAccessor,
    ILogger<ForceEnqueueCatalogVectorHandler> logger)
    : ICommandHandler<ForceEnqueueCatalogVectorCommand, ForceEnqueueCatalogVectorResult>
{
    private static readonly JsonSerializerOptions Json = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public async Task<ForceEnqueueCatalogVectorResult> Handle(
        ForceEnqueueCatalogVectorCommand request,
        CancellationToken cancellationToken)
    {
        var tenant = tenantAccessor.MultiTenantContext?.TenantInfo
            ?? throw new InvalidOperationException("Tenant is required.");
        var tenantId = tenant.Id ?? throw new InvalidOperationException("Tenant id is required.");
        var tenantKey = tenant.Identifier ?? tenantId;

        var productId = request.ProductId.Trim();
        var (_, credentials) = await GravityIntegrationAccess.LoadAsync(
            db,
            request.IntegrationId,
            logger,
            cancellationToken);

        var partition = DataIngestPartition.Key(tenantId, request.IntegrationId);
        var now = DateTimeOffset.UtcNow;

        // Always refresh from Gravity so live-catalog Force sync works even if Table has no row yet.
        var detail = await gravity.GetProductByIdAsync(credentials, productId, cancellationToken);
        if (detail is null || string.IsNullOrWhiteSpace(detail.ProductId))
            throw new KeyNotFoundException($"Catalog product '{productId}' was not found in Gravity.");

        var resolvedId = detail.ProductId.Trim();
        var contentHash = DataIngestContentHash.Compute(detail.PayloadJson);

        await tables.UpsertCatalogAsync(
            new DataIngestEntity(
                partition,
                resolvedId,
                detail.Name,
                detail.ProductStatusName,
                DataIngestPipelineStatuses.Pending,
                contentHash,
                now,
                detail.UpdatedOn,
                now,
                detail.PayloadJson),
            cancellationToken);

        var message = new CatalogVectorIngestMessage(
            tenantKey,
            request.IntegrationId,
            resolvedId,
            contentHash);
        var body = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(message, Json));
        await queue.SendAsync(DataIngestQueues.CatalogVector, body, cancellationToken);

        logger.LogInformation(
            "Force catalog vector enqueue. Tenant={Tenant} IntegrationId={IntegrationId} ProductId={ProductId} ContentHash={ContentHash} Source=gravity",
            tenantKey,
            request.IntegrationId,
            resolvedId,
            contentHash);

        return new ForceEnqueueCatalogVectorResult(request.IntegrationId, resolvedId, contentHash);
    }
}
