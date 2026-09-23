using System.Collections.Concurrent;
using System.Text;
using Azure;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Commerce.Application.Abstracts;
using Microsoft.Extensions.Logging;

namespace Commerce.Infrastructure.DataIngestion;

public sealed class AzureDataIngestPayloadBlobStore : IDataIngestPayloadBlobStore
{
    public const string ContainerName = "commercedataingest";

    private readonly BlobContainerClient _container;
    private readonly ILogger<AzureDataIngestPayloadBlobStore> _logger;
    private int _ensured;

    public AzureDataIngestPayloadBlobStore(
        string connectionString,
        ILogger<AzureDataIngestPayloadBlobStore> logger)
    {
        _logger = logger;
        var service = new BlobServiceClient(connectionString);
        _container = service.GetBlobContainerClient(ContainerName);
    }

    public async Task WriteAsync(
        string kind,
        string partitionKey,
        string rowKey,
        string payloadJson,
        CancellationToken cancellationToken = default)
    {
        await EnsureContainerAsync(cancellationToken);
        var blob = _container.GetBlobClient(BlobPath(kind, partitionKey, rowKey));
        var bytes = Encoding.UTF8.GetBytes(payloadJson ?? "{}");
        await using var stream = new MemoryStream(bytes);
        await blob.UploadAsync(
            stream,
            new BlobUploadOptions
            {
                HttpHeaders = new BlobHttpHeaders { ContentType = "application/json; charset=utf-8" },
                Conditions = null
            },
            cancellationToken);
    }

    public async Task<string?> ReadAsync(
        string kind,
        string partitionKey,
        string rowKey,
        CancellationToken cancellationToken = default)
    {
        await EnsureContainerAsync(cancellationToken);
        var blob = _container.GetBlobClient(BlobPath(kind, partitionKey, rowKey));
        try
        {
            var response = await blob.DownloadContentAsync(cancellationToken);
            return response.Value.Content.ToString();
        }
        catch (RequestFailedException ex) when (ex.Status == 404)
        {
            return null;
        }
    }

    public static string BlobPath(string kind, string partitionKey, string rowKey)
    {
        var safeKind = string.IsNullOrWhiteSpace(kind) ? "payload" : kind.Trim().ToLowerInvariant();
        return $"{safeKind}/{Uri.EscapeDataString(partitionKey.Trim())}/{Uri.EscapeDataString(rowKey.Trim())}.json";
    }

    private async Task EnsureContainerAsync(CancellationToken cancellationToken)
    {
        if (Interlocked.CompareExchange(ref _ensured, 1, 0) != 0)
            return;

        await _container.CreateIfNotExistsAsync(PublicAccessType.None, cancellationToken: cancellationToken);
        _logger.LogInformation("DataIngest payload blob container ready. Name={Container}", ContainerName);
    }
}

/// <summary>Dev fallback when Azure Blobs is not configured.</summary>
public sealed class InMemoryDataIngestPayloadBlobStore : IDataIngestPayloadBlobStore
{
    private readonly ConcurrentDictionary<string, string> _blobs = new(StringComparer.Ordinal);

    public Task WriteAsync(
        string kind,
        string partitionKey,
        string rowKey,
        string payloadJson,
        CancellationToken cancellationToken = default)
    {
        _blobs[AzureDataIngestPayloadBlobStore.BlobPath(kind, partitionKey, rowKey)] = payloadJson ?? "{}";
        return Task.CompletedTask;
    }

    public Task<string?> ReadAsync(
        string kind,
        string partitionKey,
        string rowKey,
        CancellationToken cancellationToken = default)
    {
        _blobs.TryGetValue(AzureDataIngestPayloadBlobStore.BlobPath(kind, partitionKey, rowKey), out var json);
        return Task.FromResult<string?>(json);
    }
}
