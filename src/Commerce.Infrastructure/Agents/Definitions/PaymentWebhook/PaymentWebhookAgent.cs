using Microsoft.Extensions.AI;
using Microsoft.Extensions.DependencyInjection;
using Commerce.Application.Abstracts;

namespace Commerce.Infrastructure.Agents.Definitions.PaymentWebhook;

public sealed class PaymentWebhookAgent : IAgentModule
{
    public const string KeyName = "payment-webhook";
    public const string PublishTool = "publish_order_paid";

    public string Key => KeyName;

    public static IServiceCollection Add(IServiceCollection services)
        => services.AddScoped<IAgentModule, PaymentWebhookAgent>();

    public IReadOnlyList<AITool> CreateTools(AgentDefinition definition, string tenantId, IServiceProvider services)
        => [];

    public async Task ExecuteWorkflowAsync(
        AgentDefinition definition,
        string payload,
        IServiceProvider services,
        CancellationToken cancellationToken)
    {
        if (!definition.Tools.Contains(PublishTool, StringComparer.OrdinalIgnoreCase))
        {
            return;
        }

        var orders = services.GetRequiredService<IOrderTools>();
        var output = WorkflowPayload.Json(new Dictionary<string, object?>
        {
            ["payment_id"] = WorkflowPayload.ReadPaymentId(payload),
            ["order_id"] = WorkflowPayload.ReadOrderId(payload),
            ["status"] = "paid"
        });
        foreach (var target in definition.Publishes)
        {
            await orders.PublishAsync(target, output, cancellationToken);
        }
    }
}
