using System.Text;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Commerce.Application.Abstracts;
using Commerce.Infrastructure.Accounts;
using Commerce.Infrastructure.Agents;

namespace Commerce.Infrastructure.Messaging;

/// <summary>
/// Consumes workflow agents discovered from YAML. Agent names are not registered in code.
/// </summary>
public sealed class WorkflowHostedService(
    IServiceScopeFactory scopes,
    ILogger<WorkflowHostedService> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        IReadOnlyList<AgentDefinition> workflows;
        using (var scope = scopes.CreateScope())
        {
            var store = scope.ServiceProvider.GetRequiredService<IAgentDefinitionStore>();
            var catalog = await store.ListSystemAsync(stoppingToken);
            workflows = catalog
                .Where(agent => string.Equals(agent.Kind, "workflow", StringComparison.OrdinalIgnoreCase)
                    && !string.IsNullOrWhiteSpace(agent.Queue))
                .ToList();
        }

        if (workflows.Count == 0)
        {
            logger.LogInformation("No workflow agents found under data/agents.");
            return;
        }

        await Task.WhenAll(workflows.Select(workflow => ConsumeAsync(workflow, stoppingToken)));
    }

    private async Task ConsumeAsync(AgentDefinition workflow, CancellationToken stoppingToken)
    {
        logger.LogInformation("Workflow {AgentKey} listening on {Queue}", workflow.Key, workflow.Queue);
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = scopes.CreateScope();
                var queue = scope.ServiceProvider.GetRequiredService<IMessageQueue>();
                var message = await queue.ReceiveAsync(workflow.Queue!, stoppingToken);
                if (message is null)
                {
                    await Task.Delay(TimeSpan.FromSeconds(1), stoppingToken);
                    continue;
                }

                try
                {
                    await ExecuteAsync(scope.ServiceProvider, workflow, message, stoppingToken);
                    await queue.AcknowledgeAsync(workflow.Queue!, message.MessageId, message.PopReceipt, stoppingToken);
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    logger.LogWarning(ex, "Workflow {AgentKey} failed a message. Releasing for retry.", workflow.Key);
                    await queue.ReleaseForRetryAsync(workflow.Queue!, message.MessageId, message.PopReceipt, stoppingToken);
                }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Workflow {AgentKey} failed a poll. Retrying.", workflow.Key);
                await Task.Delay(TimeSpan.FromSeconds(2), stoppingToken);
            }
        }
    }

    private static async Task ExecuteAsync(
        IServiceProvider services,
        AgentDefinition workflow,
        QueuedMessage message,
        CancellationToken cancellationToken)
    {
        var module = services.GetServices<IAgentModule>()
            .FirstOrDefault(agent => string.Equals(agent.Key, workflow.Key, StringComparison.OrdinalIgnoreCase));
        if (module is null)
        {
            throw new InvalidOperationException($"No agent module is registered for '{workflow.Key}'.");
        }

        var payload = Encoding.UTF8.GetString(message.Body.Span);
        using var _ = WorkspaceBootstrap.UseFromJsonPayload(payload);
        await module.ExecuteWorkflowAsync(workflow, payload, services, cancellationToken);
    }
}
