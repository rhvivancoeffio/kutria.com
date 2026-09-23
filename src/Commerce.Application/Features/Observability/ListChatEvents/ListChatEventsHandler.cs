using Finbuckle.MultiTenant.Abstractions;
using Microsoft.Extensions.Logging;
using Commerce.Application.Abstracts;
using Commerce.Application.Common.Abstracts;
using Commerce.Domain.Tenants;

namespace Commerce.Application.Features.Observability.ListChatEvents;

public sealed class ListChatEventsHandler(
    IChatEventTableStore tables,
    IMultiTenantContextAccessor<CommerceTenantInfo> tenantAccessor,
    ILogger<ListChatEventsHandler> logger)
    : IQueryHandler<ListChatEventsQuery, ListChatEventsResult>
{
    public async Task<ListChatEventsResult> Handle(
        ListChatEventsQuery request,
        CancellationToken cancellationToken)
    {
        var tenant = tenantAccessor.MultiTenantContext?.TenantInfo
            ?? throw new InvalidOperationException("Tenant is required.");
        var tenantId = tenant.Id
            ?? throw new InvalidOperationException("Tenant id is required.");

        var pageSize = Math.Clamp(request.PageSize, 1, 100);
        logger.LogInformation(
            "Chat events list. TenantId={TenantId} PageSize={PageSize} ThreadId={ThreadId} EventType={EventType} EmptyOnly={EmptyOnly}",
            tenantId,
            pageSize,
            request.ThreadId,
            request.EventType,
            request.EmptyOnly);

        var batch = await tables.ListAsync(
            tenantId,
            pageSize,
            request.NextToken,
            request.ThreadId,
            request.EventType,
            request.EmptyOnly,
            cancellationToken);

        var items = batch.Items
            .Select(x => new ListChatEventsItem(
                x.RowKey,
                x.EventType,
                x.ThreadId,
                x.Audience,
                x.AgentKey,
                string.IsNullOrWhiteSpace(x.Tools)
                    ? Array.Empty<string>()
                    : x.Tools.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries),
                x.LatencyMs,
                x.Empty,
                x.Error,
                x.OccurredAt))
            .ToList();

        return new ListChatEventsResult(items, batch.PageSize, batch.NextToken, batch.HasMore);
    }
}
