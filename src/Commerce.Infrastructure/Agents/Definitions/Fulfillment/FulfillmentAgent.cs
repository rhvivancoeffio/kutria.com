using Microsoft.Extensions.AI;
using Microsoft.Extensions.DependencyInjection;
using Commerce.Application.Abstracts;

namespace Commerce.Infrastructure.Agents.Definitions.Fulfillment;

public sealed class FulfillmentAgent : IAgentModule
{
    public const string KeyName = "fulfillment";

    public string Key => KeyName;

    public static IServiceCollection Add(IServiceCollection services)
        => services.AddScoped<IAgentModule, FulfillmentAgent>();

    public IReadOnlyList<AITool> CreateTools(AgentDefinition definition, string tenantId, IServiceProvider services)
        => [];

    public async Task ExecuteWorkflowAsync(
        AgentDefinition definition,
        string payload,
        IServiceProvider services,
        CancellationToken cancellationToken)
    {
        var orders = services.GetRequiredService<IOrderTools>();
        var orderId = WorkflowPayload.ReadOrderId(payload);
        var reserved = false;
        var labeled = false;

        foreach (var tool in definition.Tools)
        {
            switch (tool)
            {
                case "reserve_stock":
                    await orders.ReserveStockAsync(orderId, cancellationToken);
                    reserved = true;
                    break;
                case "create_label":
                    await orders.CreateLabelAsync(orderId, cancellationToken);
                    labeled = true;
                    break;
                case "publish_order_fulfilled":
                    var output = WorkflowPayload.Json(new Dictionary<string, object?>
                    {
                        ["order_id"] = orderId,
                        ["stock_reserved"] = reserved,
                        ["label_created"] = labeled,
                        ["status"] = "fulfilled"
                    });
                    foreach (var target in definition.Publishes)
                    {
                        await orders.PublishAsync(target, output, cancellationToken);
                    }
                    break;
            }
        }
    }
}
