namespace Commerce.Application.Abstracts;

public interface IAgentRuntime
{
    IAsyncEnumerable<AgentStreamEvent> StreamAsync(
        string prompt,
        string tenantId,
        string threadId,
        string audience = "buyer",
        ChatImageAttachment? image = null,
        string? streamId = null,
        string? agentKey = null,
        string? generationKind = null,
        string? inputSummaryJson = null,
        CancellationToken cancellationToken = default);
}
