using System.Diagnostics;
using Finbuckle.MultiTenant.Abstractions;
using Microsoft.Extensions.Logging;
using Commerce.Application.Abstracts;
using Commerce.Application.Common.Abstracts;
using Commerce.Application.Features.DataIngestion;
using Commerce.Domain.Tenants;

namespace Commerce.Application.Features.DataIngestion.ProcessCatalogImageVector;

public sealed class ProcessCatalogImageVectorHandler(
    IDataIngestTableStore tables,
    ICatalogBrainWriter writer,
    ICatalogImageVectorService imageVectors,
    IMultiTenantContextAccessor<CommerceTenantInfo> tenantAccessor,
    ILogger<ProcessCatalogImageVectorHandler> logger)
    : ICommandHandler<ProcessCatalogImageVectorCommand, ProcessCatalogImageVectorResult>
{
    public async Task<ProcessCatalogImageVectorResult> Handle(
        ProcessCatalogImageVectorCommand request,
        CancellationToken cancellationToken)
    {
        var tenant = tenantAccessor.MultiTenantContext?.TenantInfo
            ?? throw new InvalidOperationException("Tenant is required.");
        var tenantId = tenant.Id ?? tenant.Identifier
            ?? throw new InvalidOperationException("Tenant id is required.");

        var partition = DataIngestPartition.Key(tenantId, request.IntegrationId);
        var row = await tables.GetCatalogAsync(partition, request.ProductId, cancellationToken);
        if (row is null)
        {
            logger.LogWarning(
                "Catalog image vector skipped missing row. IntegrationId={IntegrationId} ProductId={ProductId}",
                request.IntegrationId,
                request.ProductId);
            return new ProcessCatalogImageVectorResult(request.IntegrationId, request.ProductId, "missing", 0);
        }

        var currentHash = row.ContentHash ?? DataIngestContentHash.Compute(row.PayloadJson);
        if (!string.Equals(currentHash, request.ExpectedContentHash, StringComparison.OrdinalIgnoreCase))
        {
            logger.LogInformation(
                "Catalog image vector ack stale message. ProductId={ProductId} Expected={Expected} Current={Current}",
                request.ProductId,
                request.ExpectedContentHash,
                currentHash);
            return new ProcessCatalogImageVectorResult(request.IntegrationId, request.ProductId, "stale", 0);
        }

        var skus = request.Skus
            .Where(s => !string.IsNullOrWhiteSpace(s))
            .Select(s => s.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();
        if (skus.Length == 0)
        {
            logger.LogWarning(
                "Catalog image vector skipped empty sku list. ProductId={ProductId} ImageUrl={ImageUrl}",
                request.ProductId,
                Truncate(request.ImageUrl));
            return new ProcessCatalogImageVectorResult(request.IntegrationId, request.ProductId, "empty", 0);
        }

        var sw = Stopwatch.StartNew();
        var resolved = await imageVectors.ResolveAsync(tenantId, request.ImageUrl, cancellationToken);
        sw.Stop();

        if (!resolved.HasVector)
        {
            logger.LogWarning(
                "Catalog image vector caption/embed failed. ProductId={ProductId} ImageUrl={ImageUrl} ElapsedMs={ElapsedMs} SkuCount={SkuCount}",
                request.ProductId,
                Truncate(request.ImageUrl),
                sw.ElapsedMilliseconds,
                skus.Length);
            return new ProcessCatalogImageVectorResult(request.IntegrationId, request.ProductId, "no_vector", 0);
        }

        logger.LogInformation(
            "Catalog image vector resolved. ProductId={ProductId} ImageUrl={ImageUrl} ImageDims={ImageDims} ElapsedMs={ElapsedMs} CacheOrCompute Caption={Caption}",
            request.ProductId,
            Truncate(request.ImageUrl),
            resolved.Vector!.Count,
            sw.ElapsedMilliseconds,
            Truncate(resolved.Caption, 120));

        var mapped = GravityCatalogBrainMapper.MapProduct(row.PayloadJson);
        var primary = mapped.Skus.FirstOrDefault(s =>
                skus.Contains(s.Sku, StringComparer.OrdinalIgnoreCase))
            ?? mapped.Skus.FirstOrDefault();
        var sku = primary?.Sku ?? skus[0];

        await writer.UpsertImageDocumentAsync(
            new CatalogImageDocument(
                tenantId,
                primary?.ProductId ?? request.ProductId,
                sku,
                request.ImageUrl,
                resolved.Vector,
                primary?.Title,
                primary?.Brand,
                primary?.Seller,
                primary?.Price ?? 0,
                primary?.IsActive ?? true,
                primary?.Snapshot.Stock ?? 0,
                primary?.SkuId ?? sku,
                primary?.SellerId),
            cancellationToken);

        logger.LogInformation(
            "Catalog image vector indexed. ProductId={ProductId} Sku={Sku} RelatedSkuCount={RelatedSkuCount} ImageUrl={ImageUrl}",
            request.ProductId,
            sku,
            skus.Length,
            Truncate(request.ImageUrl));

        return new ProcessCatalogImageVectorResult(request.IntegrationId, request.ProductId, "indexed", 1);
    }

    private static string Truncate(string? value, int max = 200)
    {
        if (string.IsNullOrEmpty(value))
            return string.Empty;
        return value.Length <= max ? value : value[..max] + "…";
    }
}
