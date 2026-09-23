using System.Text;
using System.Text.Json;
using Finbuckle.MultiTenant.Abstractions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Commerce.Application.Abstracts;
using Commerce.Application.Common.Abstracts;
using Commerce.Application.Features.DataIngestion;
using Commerce.Domain.Tenants;

namespace Commerce.Application.Features.DataIngestion.ProcessCatalogVectorIngest;

public sealed class ProcessCatalogVectorIngestHandler(
    IDataIngestTableStore tables,
    ICatalogBrainWriter writer,
    IIngestedCatalogMemory ingestedCatalog,
    IMessageQueue queue,
    IOptions<CatalogImageOptions> imageOptions,
    IMultiTenantContextAccessor<CommerceTenantInfo> tenantAccessor,
    ILogger<ProcessCatalogVectorIngestHandler> logger)
    : ICommandHandler<ProcessCatalogVectorIngestCommand, ProcessCatalogVectorIngestResult>
{
    private const int SkuUpsertMaxDegree = 4;

    private static readonly JsonSerializerOptions Json = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public async Task<ProcessCatalogVectorIngestResult> Handle(
        ProcessCatalogVectorIngestCommand request,
        CancellationToken cancellationToken)
    {
        var tenant = tenantAccessor.MultiTenantContext?.TenantInfo
            ?? throw new InvalidOperationException("Tenant is required.");
        var tenantId = tenant.Id ?? tenant.Identifier
            ?? throw new InvalidOperationException("Tenant id is required.");
        var tenantKey = tenant.Identifier ?? tenantId;

        var partition = DataIngestPartition.Key(tenantId, request.IntegrationId);
        var row = await tables.GetCatalogAsync(partition, request.ProductId, cancellationToken);
        if (row is null)
        {
            logger.LogWarning(
                "Catalog vector ingest skipped missing row. IntegrationId={IntegrationId} ProductId={ProductId}",
                request.IntegrationId,
                request.ProductId);
            return new ProcessCatalogVectorIngestResult(request.IntegrationId, request.ProductId, "missing", 0);
        }

        var currentHash = row.ContentHash ?? DataIngestContentHash.Compute(row.PayloadJson);

        // Stale message: a newer sync already superseded this hash.
        if (!string.Equals(currentHash, request.ExpectedContentHash, StringComparison.OrdinalIgnoreCase))
        {
            logger.LogInformation(
                "Catalog vector ingest ack stale message. ProductId={ProductId} Expected={Expected} Current={Current}",
                request.ProductId,
                request.ExpectedContentHash,
                currentHash);
            return new ProcessCatalogVectorIngestResult(request.IntegrationId, request.ProductId, "stale", 0);
        }

        // Idempotent no-op.
        if (string.Equals(row.Status, DataIngestPipelineStatuses.Indexed, StringComparison.OrdinalIgnoreCase)
            && string.Equals(row.ContentHash, request.ExpectedContentHash, StringComparison.OrdinalIgnoreCase))
        {
            return new ProcessCatalogVectorIngestResult(request.IntegrationId, request.ProductId, "noop", 0);
        }

        if (request.DequeueCount >= DataIngestPipelineStatuses.PoisonDequeueCount)
        {
            await tables.PatchCatalogPipelineAsync(
                partition,
                request.ProductId,
                DataIngestPipelineStatuses.Failed,
                currentHash,
                DateTimeOffset.UtcNow,
                cancellationToken);
            logger.LogWarning(
                "Catalog vector ingest poison. ProductId={ProductId} DequeueCount={DequeueCount}",
                request.ProductId,
                request.DequeueCount);
            return new ProcessCatalogVectorIngestResult(request.IntegrationId, request.ProductId, "poison", 0);
        }

        var now = DateTimeOffset.UtcNow;
        await tables.PatchCatalogPipelineAsync(
            partition,
            request.ProductId,
            DataIngestPipelineStatuses.Processing,
            currentHash,
            now,
            cancellationToken);

        var maxImages = Math.Clamp(imageOptions.Value.MaxImagesPerProduct, 1, 20);
        var mapped = GravityCatalogBrainMapper.MapProduct(row.PayloadJson, maxImages);
        var skus = mapped.Skus;
        if (skus.Count == 0)
        {
            await tables.PatchCatalogPipelineAsync(
                partition,
                request.ProductId,
                DataIngestPipelineStatuses.Indexed,
                currentHash,
                DateTimeOffset.UtcNow,
                cancellationToken);
            logger.LogWarning(
                "Catalog vector ingest no SKUs. ProductId={ProductId} marked indexed.",
                request.ProductId);
            return new ProcessCatalogVectorIngestResult(request.IntegrationId, request.ProductId, "empty", 0);
        }

        try
        {
            await Parallel.ForEachAsync(
                skus,
                new ParallelOptions
                {
                    MaxDegreeOfParallelism = SkuUpsertMaxDegree,
                    CancellationToken = cancellationToken
                },
                async (item, ct) =>
                {
                    await writer.UpsertAsync(
                        new CatalogProductDocument(
                            tenantId,
                            item.Sku,
                            item.Title,
                            item.Description,
                            item.Category,
                            item.Price,
                            item.IsActive,
                            item.ImageUrl,
                            item.Brand,
                            item.Seller,
                            item.ProductId,
                            item.SkuId,
                            item.SellerId,
                            item.Snapshot.Stock,
                            item.OptionKeys,
                            item.OptionPairs),
                        ct);
                });

            ingestedCatalog.UpsertSkus(tenantId, skus.Select(x => x.Snapshot).ToList());

            await tables.PatchCatalogPipelineAsync(
                partition,
                request.ProductId,
                DataIngestPipelineStatuses.Indexed,
                currentHash,
                DateTimeOffset.UtcNow,
                cancellationToken);

            var allSkus = (IReadOnlyList<string>)skus
                .Select(s => s.Sku)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToArray();
            var imageJobs = mapped.CanonicalImageUrls
                .Where(u => !string.IsNullOrWhiteSpace(u))
                .Select(u => CatalogImageUrl.Normalize(u) ?? u.Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .Select(url => (ImageUrl: url, Skus: allSkus))
                .ToList();

            var enqueued = 0;
            foreach (var job in imageJobs)
            {
                try
                {
                    var message = new CatalogImageVectorMessage(
                        tenantKey,
                        request.IntegrationId,
                        request.ProductId,
                        job.ImageUrl,
                        job.Skus,
                        currentHash);
                    var body = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(message, Json));
                    await queue.SendAsync(DataIngestQueues.CatalogImage, body, cancellationToken);
                    enqueued++;
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    logger.LogWarning(
                        ex,
                        "Catalog image vector enqueue failed. ProductId={ProductId} ImageUrl={ImageUrl} SkuCount={SkuCount}",
                        request.ProductId,
                        job.ImageUrl,
                        job.Skus.Count);
                }
            }

            logger.LogInformation(
                "Catalog vector ingest indexed. ProductId={ProductId} SkuCount={SkuCount} ImageUrlsEnqueued={ImageUrlsEnqueued} CanonicalImageUrls={CanonicalImageUrls} MaxImagesPerProduct={MaxImagesPerProduct}",
                request.ProductId,
                skus.Count,
                enqueued,
                imageJobs.Count,
                maxImages);

            return new ProcessCatalogVectorIngestResult(request.IntegrationId, request.ProductId, "indexed", skus.Count);
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("not configured", StringComparison.OrdinalIgnoreCase))
        {
            await tables.PatchCatalogPipelineAsync(
                partition,
                request.ProductId,
                DataIngestPipelineStatuses.Failed,
                currentHash,
                DateTimeOffset.UtcNow,
                cancellationToken);
            logger.LogWarning(
                ex,
                "Catalog vector ingest skipped (embeddings not configured). ProductId={ProductId}",
                request.ProductId);
            return new ProcessCatalogVectorIngestResult(request.IntegrationId, request.ProductId, "unconfigured", 0);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            await tables.PatchCatalogPipelineAsync(
                partition,
                request.ProductId,
                DataIngestPipelineStatuses.Failed,
                currentHash,
                DateTimeOffset.UtcNow,
                cancellationToken);
            logger.LogWarning(
                ex,
                "Catalog vector ingest failed. ProductId={ProductId}",
                request.ProductId);
            throw;
        }
    }
}
