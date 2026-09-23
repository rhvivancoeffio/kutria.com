using Azure;
using Azure.Data.Tables;
using Commerce.Application.Abstracts;
using Microsoft.Extensions.Logging;

namespace Commerce.Infrastructure.Agents.Events;

public sealed class AzureAgentGenerationAuditStore : IAgentGenerationAuditStore
{
    public const string TableName = "commerceagentgenerations";

    private readonly TableClient _table;
    private readonly ILogger<AzureAgentGenerationAuditStore> _logger;
    private int _ensured;

    public AzureAgentGenerationAuditStore(string connectionString, ILogger<AzureAgentGenerationAuditStore> logger)
    {
        _logger = logger;
        _table = new TableServiceClient(connectionString).GetTableClient(TableName);
    }

    public async Task UpsertAsync(AgentGenerationAuditMessage message, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(message.TenantId) || string.IsNullOrWhiteSpace(message.ProcessId))
            return;

        await EnsureTableAsync(cancellationToken);
        AgentGenerationAuditEntity entity;
        try
        {
            var existing = await _table.GetEntityAsync<AgentGenerationAuditEntity>(
                message.TenantId.Trim(),
                message.ProcessId.Trim(),
                cancellationToken: cancellationToken);
            entity = existing.Value;
        }
        catch (RequestFailedException ex) when (ex.Status == 404)
        {
            entity = new AgentGenerationAuditEntity
            {
                PartitionKey = message.TenantId.Trim(),
                RowKey = message.ProcessId.Trim(),
                StartedAtUtc = message.OccurredAtUtc
            };
        }

        entity.AgentKey = message.AgentKey;
        entity.Kind = message.Kind;
        entity.Audience = message.Audience;
        entity.Status = message.Status;
        if (!string.IsNullOrWhiteSpace(message.InputSummaryJson))
            entity.InputSummaryJson = message.InputSummaryJson;
        if (!string.IsNullOrWhiteSpace(message.Error))
            entity.Error = message.Error;
        if (!string.IsNullOrWhiteSpace(message.OutputHash))
            entity.OutputHash = message.OutputHash;

        var terminal = string.Equals(message.Status, "done", StringComparison.OrdinalIgnoreCase)
            || string.Equals(message.Status, "error", StringComparison.OrdinalIgnoreCase);
        if (terminal)
            entity.CompletedAtUtc = message.OccurredAtUtc;
        if (entity.StartedAtUtc == default)
            entity.StartedAtUtc = message.OccurredAtUtc;

        await _table.UpsertEntityAsync(entity, TableUpdateMode.Replace, cancellationToken);
        _logger.LogInformation(
            "Agent generation audit upserted. TenantId={TenantId} ProcessId={ProcessId} Status={Status} AgentKey={AgentKey}",
            entity.PartitionKey,
            entity.RowKey,
            entity.Status,
            entity.AgentKey);
    }

    private async Task EnsureTableAsync(CancellationToken cancellationToken)
    {
        if (Interlocked.CompareExchange(ref _ensured, 1, 0) == 0)
            await _table.CreateIfNotExistsAsync(cancellationToken);
    }
}

public sealed class AgentGenerationAuditEntity : ITableEntity
{
    public string PartitionKey { get; set; } = string.Empty;
    public string RowKey { get; set; } = string.Empty;
    public DateTimeOffset? Timestamp { get; set; }
    public ETag ETag { get; set; }
    public string? AgentKey { get; set; }
    public string? Kind { get; set; }
    public string? Audience { get; set; }
    public string? Status { get; set; }
    public DateTimeOffset StartedAtUtc { get; set; }
    public DateTimeOffset? CompletedAtUtc { get; set; }
    public string? InputSummaryJson { get; set; }
    public string? Error { get; set; }
    public string? OutputHash { get; set; }
}

public sealed class InMemoryAgentGenerationAuditStore : IAgentGenerationAuditStore
{
    private readonly Dictionary<string, AgentGenerationAuditMessage> _rows = new(StringComparer.Ordinal);
    private readonly object _gate = new();

    public Task UpsertAsync(AgentGenerationAuditMessage message, CancellationToken cancellationToken = default)
    {
        var key = $"{message.TenantId}:{message.ProcessId}";
        lock (_gate)
            _rows[key] = message;
        return Task.CompletedTask;
    }
}
