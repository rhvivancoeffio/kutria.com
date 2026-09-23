using System.Text.Json;
using Commerce.Application.Abstracts;
using Commerce.Infrastructure.Agents;
using Finbuckle.MultiTenant.Abstractions;
using Commerce.Domain.Tenants;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;
using McpTool = ModelContextProtocol.Protocol.Tool;

namespace Commerce.Mcp.Mcp;

public sealed class McpToolsHandler(
    IToolCatalog catalog,
    IMcpToolInvoker toolInvoker,
    IMultiTenantContextAccessor<CommerceTenantInfo> tenantAccessor,
    IServiceScopeFactory scopeFactory,
    ILogger<McpToolsHandler> logger)
{
    public async ValueTask<ListToolsResult> HandleListToolsAsync(
        RequestContext<ListToolsRequestParams> request,
        CancellationToken cancellationToken)
    {
        var tools = await catalog.GetMcpToolsAsync(cancellationToken);
        var mcpTools = tools.Select(t => new McpTool
        {
            Name = t.Name,
            Description = t.Description,
            InputSchema = t.InputSchema.ValueKind == JsonValueKind.Undefined
                ? JsonSerializer.Deserialize<JsonElement>("""{"type":"object","properties":{}}""")
                : t.InputSchema
        }).ToList();

        return new ListToolsResult { Tools = mcpTools };
    }

    public async ValueTask<CallToolResult> HandleCallToolAsync(
        RequestContext<CallToolRequestParams> request,
        CancellationToken cancellationToken)
    {
        var tenant = tenantAccessor.MultiTenantContext?.TenantInfo;
        if (tenant is null)
        {
            throw new InvalidOperationException(
                "Tenant context required. Authenticate with x-api-key or a Bearer token that includes tenant_id.");
        }

        var name = request.Params?.Name;
        if (string.IsNullOrEmpty(name))
            throw new ArgumentException("Tool name is required.");

        await using var scope = scopeFactory.CreateAsyncScope();
        try
        {
            var text = await toolInvoker.CallAsync(
                name,
                request.Params?.Arguments,
                tenant.Id,
                scope.ServiceProvider,
                cancellationToken);

            return new CallToolResult
            {
                Content = [new TextContentBlock { Text = text }]
            };
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "MCP CallTool failed for {Tool}", name);
            return new CallToolResult
            {
                IsError = true,
                Content = [new TextContentBlock { Text = ex.Message }]
            };
        }
    }
}
