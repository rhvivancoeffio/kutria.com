using System.Text;
using System.Text.Json;
using Finbuckle.MultiTenant.Abstractions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Commerce.Application.Abstracts;
using Commerce.Application.Common.Abstracts;
using Commerce.Application.Features.DataIngestion;
using Commerce.Domain.Tenants;

namespace Commerce.Application.Features.DataIngestion.EnqueueDataIngest;

public sealed class EnqueueDataIngestHandler(
    ICommerceDbContext db,
    IMessageQueue queue,
    IMultiTenantContextAccessor<CommerceTenantInfo> tenantAccessor,
    ILogger<EnqueueDataIngestHandler> logger)
    : ICommandHandler<EnqueueDataIngestCommand, EnqueueDataIngestResult>
{
    public async Task<EnqueueDataIngestResult> Handle(EnqueueDataIngestCommand request, CancellationToken cancellationToken)
    {
        var tenant = tenantAccessor.MultiTenantContext?.TenantInfo
            ?? throw new InvalidOperationException("Tenant is required.");

        logger.LogInformation(
            "DataIngest enqueue requested. Tenant={Tenant} IntegrationId={IntegrationId} Kind={Kind}",
            tenant.Identifier ?? tenant.Id,
            request.IntegrationId,
            request.Kind ?? DataIngestKinds.All);

        var integration = await db.Integrations.AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == request.IntegrationId, cancellationToken)
            ?? throw new KeyNotFoundException($"Integration {request.IntegrationId} was not found.");

        logger.LogInformation(
            "DataIngest enqueue integration found. Provider={Provider} Name={Name} IsActive={IsActive} SettingsLength={SettingsLength}",
            integration.Provider,
            integration.Name,
            integration.IsActive,
            integration.SettingsJson?.Length ?? 0);

        if (!integration.IsActive)
            throw new InvalidOperationException("Integration is not active.");

        if (!DataIngestCredentials.TryParse(integration.SettingsJson, out var credentials, out var reason))
        {
            logger.LogWarning(
                "DataIngest enqueue credentials parse failed. IntegrationId={IntegrationId} Reason={Reason}",
                integration.Id,
                reason);
            throw new InvalidOperationException($"Integration does not have Gravity API credentials (url + apiKey + organizationId). {reason}");
        }

        logger.LogInformation(
            "DataIngest enqueue credentials OK. BaseUrl={BaseUrl} ApiKey={ApiKeyMask}",
            credentials.BaseUrl,
            DataIngestCredentials.MaskApiKey(credentials.ApiKey));

        var kinds = ResolveKinds(request.Kind);
        var tenantKey = tenant.Identifier ?? tenant.Id
            ?? throw new InvalidOperationException("Tenant identifier is required.");

        foreach (var kind in kinds)
        {
            var message = new DataIngestMessage(tenantKey, integration.Id, kind);
            var body = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(message));
            await queue.SendAsync(DataIngestQueues.Ingest, body, cancellationToken);
            logger.LogInformation(
                "DataIngest message enqueued. Queue={Queue} Tenant={Tenant} IntegrationId={IntegrationId} Kind={Kind}",
                DataIngestQueues.Ingest,
                tenantKey,
                integration.Id,
                kind);
        }

        return new EnqueueDataIngestResult(integration.Id, kinds);
    }

    private static IReadOnlyList<string> ResolveKinds(string? kind)
    {
        if (string.IsNullOrWhiteSpace(kind)
            || string.Equals(kind, DataIngestKinds.All, StringComparison.OrdinalIgnoreCase))
        {
            return [DataIngestKinds.Catalog, DataIngestKinds.Orders];
        }

        return [kind.Trim().ToLowerInvariant()];
    }
}
