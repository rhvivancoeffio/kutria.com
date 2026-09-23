using Carter;
using Finbuckle.MultiTenant.Abstractions;
using Microsoft.Extensions.Options;
using Commerce.Application.Abstracts;
using Commerce.Application.Configuration;
using Commerce.Domain.Tenants;

namespace Commerce.Api.Modules;

public sealed class EventStreamsModule : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        Map(app.MapGroup("/t/{__tenant__}/event-streams").WithTags("EventStreams").RequireCors("Chat"));
    }

    private static void Map(RouteGroupBuilder group)
    {
        group.MapGet("/settings", GetSettings);
        group.MapGet("/{streamId}/events", ListEventsAsync);
    }

    private static IResult GetSettings(IOptions<EventStreamsOptions> options)
    {
        var opts = options.Value ?? new EventStreamsOptions();
        return Results.Ok(new
        {
            transport = opts.NormalizedTransport,
            pollIntervalMs = opts.ClampedPollIntervalMs,
            ttl = opts.Ttl
        });
    }

    private static async Task<IResult> ListEventsAsync(
        string streamId,
        string? after,
        int? take,
        IEventStreamStore store,
        IMultiTenantContextAccessor<CommerceTenantInfo> tenants,
        CancellationToken cancellationToken)
    {
        var tenant = tenants.MultiTenantContext?.TenantInfo;
        if (tenant?.Id is null)
            return Results.BadRequest(new { error = "Tenant is required." });

        if (string.IsNullOrWhiteSpace(streamId))
            return Results.BadRequest(new { error = "streamId is required." });

        var batch = await store.ListSinceAsync(
            tenant.Id,
            streamId.Trim(),
            string.IsNullOrWhiteSpace(after) ? null : after.Trim(),
            take ?? 50,
            cancellationToken);

        return Results.Ok(new
        {
            streamId = streamId.Trim(),
            nextAfter = batch.NextAfter,
            completed = batch.Completed,
            events = batch.Items.Select(e => MapEvent(e))
        });
    }

    private static object MapEvent(StreamEvent e)
    {
        string? toolName = null;
        string? agentKey = null;
        string? threadId = null;
        if (!string.IsNullOrWhiteSpace(e.Data))
        {
            try
            {
                using var doc = System.Text.Json.JsonDocument.Parse(e.Data);
                if (doc.RootElement.TryGetProperty("toolName", out var t) && t.ValueKind == System.Text.Json.JsonValueKind.String)
                    toolName = t.GetString();
                if (doc.RootElement.TryGetProperty("agentKey", out var a) && a.ValueKind == System.Text.Json.JsonValueKind.String)
                    agentKey = a.GetString();
                if (doc.RootElement.TryGetProperty("threadId", out var th) && th.ValueKind == System.Text.Json.JsonValueKind.String)
                    threadId = th.GetString();
            }
            catch
            {
                // Data may be opaque JSON for other features.
            }
        }

        return new
        {
            type = e.Type,
            text = e.Text,
            toolName,
            agentKey,
            threadId,
            rowKey = e.RowKey,
            data = e.Data
        };
    }
}
