using System.Text.Json;
using Azure;
using Azure.Data.Tables;
using Commerce.Application.Abstracts;
using Microsoft.Extensions.Logging;

namespace Commerce.Infrastructure.Agents.Profile;

/// <summary>
/// Buyer session profile in Azure Table Storage.
/// PartitionKey = tenantId, RowKey = threadId.
/// </summary>
public sealed class AzureSessionBuyerProfileStore : ISessionBuyerProfileStore
{
    public const string TableName = "commercebuyerprofiles";

    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    private readonly TableClient _table;
    private readonly ILogger<AzureSessionBuyerProfileStore> _logger;
    private int _ensured;

    public AzureSessionBuyerProfileStore(
        string connectionString,
        ILogger<AzureSessionBuyerProfileStore> logger)
    {
        _logger = logger;
        var service = new TableServiceClient(connectionString);
        _table = service.GetTableClient(TableName);
    }

    public async Task<SessionBuyerProfile> GetAsync(
        string tenantId,
        string threadId,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(tenantId) || string.IsNullOrWhiteSpace(threadId))
            return SessionBuyerProfile.Empty(tenantId ?? string.Empty, threadId ?? string.Empty);

        await EnsureTableAsync(cancellationToken);
        try
        {
            var response = await _table.GetEntityAsync<BuyerProfileTableEntity>(
                Partition(tenantId),
                RowKey(threadId),
                cancellationToken: cancellationToken);
            return Deserialize(response.Value.ProfileJson, tenantId, threadId);
        }
        catch (RequestFailedException ex) when (ex.Status == 404)
        {
            return SessionBuyerProfile.Empty(tenantId, threadId);
        }
        catch (JsonException ex)
        {
            _logger.LogWarning(
                ex,
                "Buyer profile ignored malformed payload for tenant {TenantId} thread {ThreadId}",
                tenantId,
                threadId);
            return SessionBuyerProfile.Empty(tenantId, threadId);
        }
    }

    public async Task SaveAsync(
        SessionBuyerProfile profile,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(profile.TenantId) || string.IsNullOrWhiteSpace(profile.ThreadId))
            throw new ArgumentException("TenantId and ThreadId are required.");

        await EnsureTableAsync(cancellationToken);
        var toSave = profile with { UpdatedAt = DateTimeOffset.UtcNow };
        var entity = new BuyerProfileTableEntity
        {
            PartitionKey = Partition(toSave.TenantId),
            RowKey = RowKey(toSave.ThreadId),
            ThreadId = toSave.ThreadId.Trim(),
            ProfileJson = JsonSerializer.Serialize(toSave, Json),
            UpdatedAt = toSave.UpdatedAt
        };
        await _table.UpsertEntityAsync(entity, TableUpdateMode.Replace, cancellationToken);
    }

    private async Task EnsureTableAsync(CancellationToken cancellationToken)
    {
        if (Interlocked.CompareExchange(ref _ensured, 1, 0) == 0)
            await _table.CreateIfNotExistsAsync(cancellationToken);
    }

    private static string Partition(string tenantId) => tenantId.Trim();

    private static string RowKey(string threadId) => threadId.Trim();

    private static SessionBuyerProfile Deserialize(string? json, string tenantId, string threadId)
    {
        if (string.IsNullOrWhiteSpace(json))
            return SessionBuyerProfile.Empty(tenantId, threadId);
        return JsonSerializer.Deserialize<SessionBuyerProfile>(json, Json)
               ?? SessionBuyerProfile.Empty(tenantId, threadId);
    }
}

public sealed class BuyerProfileTableEntity : ITableEntity
{
    public string PartitionKey { get; set; } = string.Empty;
    public string RowKey { get; set; } = string.Empty;
    public DateTimeOffset? Timestamp { get; set; }
    public ETag ETag { get; set; }
    public string? ThreadId { get; set; }
    public string? ProfileJson { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
}
