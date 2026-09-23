using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Commerce.Application.Abstracts;
using Microsoft.Extensions.Logging;

namespace Commerce.Infrastructure.Agents.Events;

public sealed class QueueAgentGenerationAuditPublisher(
    IMessageQueue queue,
    ILogger<QueueAgentGenerationAuditPublisher> logger) : IAgentGenerationAuditPublisher
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    public async Task PublishAsync(AgentGenerationAuditMessage message, CancellationToken cancellationToken = default)
    {
        try
        {
            var body = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(message, Json));
            await queue.SendAsync(AgentAuditQueues.Generations, body, cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            logger.LogWarning(
                ex,
                "Agent generation audit publish failed. ProcessId={ProcessId} AgentKey={AgentKey} Status={Status}",
                message.ProcessId,
                message.AgentKey,
                message.Status);
        }
    }
}

public static class AgentGenerationAuditHash
{
    public static string? Sha256(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return null;
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(text));
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }
}
