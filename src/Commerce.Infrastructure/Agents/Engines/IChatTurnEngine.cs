using Microsoft.Extensions.AI;
using Commerce.Application.Abstracts;

namespace Commerce.Infrastructure.Agents.Engines;

/// <summary>One chat routing strategy (buyer-switch or triage handoff).</summary>
public interface IChatTurnEngine
{
    bool CanHandle(string audience);

    IAsyncEnumerable<AgentStreamEvent> StreamAsync(
        IChatClient chat,
        IReadOnlyList<AgentDefinition> catalog,
        string prompt,
        string tenantId,
        string threadId,
        string audience,
        ChatImageAttachment? image,
        IReadOnlyList<ChatThreadMessage> history,
        CancellationToken cancellationToken);
}
