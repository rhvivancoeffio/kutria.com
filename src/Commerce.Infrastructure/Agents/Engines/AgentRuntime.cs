using System.Diagnostics;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Commerce.Application.Abstracts;
using Commerce.Infrastructure.Agents;
using Commerce.Infrastructure.Agents.Events;
using Commerce.Infrastructure.EventStreams;

namespace Commerce.Infrastructure.Agents.Engines;

/// <summary>
/// Facade: loads catalog, scopes the turn, then delegates to buyer-switch or triage handoff.
/// </summary>
public sealed class AgentRuntime(
    IAgentDefinitionStore store,
    IServiceProvider services,
    IChatThreadStore threadStore,
    IChatEventPublisher chatEvents,
    IEventStreamStore eventStreams,
    IAgentGenerationAuditPublisher generationAudit,
    IEnumerable<IChatTurnEngine> engines,
    ILogger<AgentRuntime> logger) : IAgentRuntime
{
    public async IAsyncEnumerable<AgentStreamEvent> StreamAsync(
        string prompt,
        string tenantId,
        string threadId,
        string audience = "buyer",
        ChatImageAttachment? image = null,
        string? streamId = null,
        string? agentKey = null,
        string? generationKind = null,
        string? inputSummaryJson = null,
        [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var sw = Stopwatch.StartNew();
        ChatTurnScope.Current = new ChatTurnState(
            tenantId,
            audience,
            threadId,
            image?.AttachmentId,
            image?.Url,
            image?.ContentType);
        var toolsSeen = new List<string>();
        string? turnError = null;
        string? answeredBy = null;
        var empty = true;
        var sawTerminal = false;
        var effectivePrompt = AgentChatSupport.BuildPrompt(prompt, image);
        var persistStreamId = string.IsNullOrWhiteSpace(streamId) ? null : streamId.Trim();
        var pinnedAgentKey = string.IsNullOrWhiteSpace(agentKey) ? null : agentKey.Trim();
        var kind = string.IsNullOrWhiteSpace(generationKind) ? "chat" : generationKind.Trim();
        string? outputJson = null;
        var auditStarted = false;

        try
        {
            List<AgentDefinition>? catalog = null;
            string? loadError = null;
            try
            {
                catalog = await LoadAudienceAsync(audience, pinnedAgentKey, cancellationToken);
            }
            catch (Exception ex)
            {
                loadError = ex.Message;
            }

            if (loadError is not null)
            {
                logger.LogWarning("Chat load failed for tenant {TenantId}: {Error}", tenantId, loadError);
                turnError = loadError;
                yield return await EmitAsync(
                    AgentStreamEvent.Error(loadError), tenantId, persistStreamId, threadId, cancellationToken);
                sawTerminal = true;
                yield break;
            }

            if (catalog is null || catalog.Count == 0)
            {
                turnError = pinnedAgentKey is null
                    ? $"No conversational agents for audience '{audience}'."
                    : $"Agent '{pinnedAgentKey}' not found for audience '{audience}'.";
                logger.LogWarning(
                    "Chat has no conversational agents for audience {Audience} tenant {TenantId} agent {AgentKey}",
                    audience,
                    tenantId,
                    pinnedAgentKey ?? "(any)");
                yield return await EmitAsync(
                    AgentStreamEvent.Error(turnError), tenantId, persistStreamId, threadId, cancellationToken);
                sawTerminal = true;
                yield break;
            }

            if (persistStreamId is not null)
            {
                var startKey = pinnedAgentKey ?? catalog[0].Key;
                await PublishGenerationAuditAsync(
                    tenantId,
                    persistStreamId,
                    startKey,
                    audience,
                    kind,
                    "started",
                    inputSummaryJson,
                    error: null,
                    outputHash: null,
                    cancellationToken);
                auditStarted = true;
            }

            logger.LogInformation(
                "Chat turn {ThreadId} audience {Audience} agents {Agents} prompt {Prompt} image {HasImage}",
                threadId,
                audience,
                string.Join(", ", catalog.Select(agent => agent.Key)),
                AgentChatSupport.Preview(effectivePrompt),
                image is not null);

            var chat = services.GetService<IChatClient>();
            if (chat is null)
            {
                logger.LogWarning("Chat has no IChatClient. Returning placeholder for {AgentKey}", catalog[0].Key);
                var fallback = catalog[0];
                if (AgentOutput.TryRead(AgentOutput.Placeholder(fallback.Name), out var placeholder, out var message, out _))
                {
                    empty = false;
                    answeredBy = fallback.Key;
                    outputJson = placeholder;
                    await PersistTurnAsync(
                        tenantId, audience, threadId, [], effectivePrompt, message ?? fallback.Name, cancellationToken);
                    yield return await EmitAsync(
                        AgentStreamEvent.Token(message ?? fallback.Name),
                        tenantId,
                        persistStreamId,
                        threadId,
                        cancellationToken);
                    yield return await EmitAsync(
                        AgentStreamEvent.Output(placeholder),
                        tenantId,
                        persistStreamId,
                        threadId,
                        cancellationToken);
                }

                yield return await EmitAsync(
                    AgentStreamEvent.Done(fallback.Key, threadId),
                    tenantId,
                    persistStreamId,
                    threadId,
                    cancellationToken);
                sawTerminal = true;
                yield break;
            }

            var engine = engines.FirstOrDefault(e => e.CanHandle(audience));
            if (engine is null)
            {
                turnError = $"No chat engine for audience '{audience}'.";
                yield return await EmitAsync(
                    AgentStreamEvent.Error(turnError), tenantId, persistStreamId, threadId, cancellationToken);
                sawTerminal = true;
                yield break;
            }

            logger.LogInformation(
                "Chat turn {ThreadId} engine {Engine}",
                threadId,
                engine.GetType().Name);

            var history = await threadStore.GetAsync(tenantId, audience, threadId, cancellationToken);
            ToolCallTurn.Current.Value = new ToolCallTurn
            {
                TenantId = tenantId,
                ThreadId = threadId,
                Audience = audience
            };
            try
            {
                await foreach (var evt in engine.StreamAsync(
                    chat, catalog, effectivePrompt, tenantId, threadId, audience, image, history, cancellationToken))
                {
                    switch (evt.Type)
                    {
                        case "tool" when !string.IsNullOrWhiteSpace(evt.ToolName):
                            toolsSeen.Add(evt.ToolName);
                            break;
                        case "error":
                            turnError = evt.Text;
                            sawTerminal = true;
                            break;
                        case "done":
                            empty = false;
                            answeredBy = evt.AgentKey;
                            sawTerminal = true;
                            break;
                        case "output" when !string.IsNullOrWhiteSpace(evt.Text):
                            empty = false;
                            outputJson = evt.Text;
                            break;
                        case "token":
                            empty = false;
                            break;
                    }

                    yield return await EmitAsync(evt, tenantId, persistStreamId, threadId, cancellationToken);
                }
            }
            finally
            {
                ToolCallTurn.Current.Value = null;
            }

            if (!sawTerminal && persistStreamId is not null)
            {
                var missing = turnError ?? "Chat turn ended without a terminal event.";
                yield return await EmitAsync(
                    AgentStreamEvent.Error(missing), tenantId, persistStreamId, threadId, cancellationToken);
                sawTerminal = true;
                turnError ??= missing;
            }
        }
        finally
        {
            if (!sawTerminal && persistStreamId is not null)
            {
                try
                {
                    await EventStreamWriter.ErrorAsync(
                        eventStreams,
                        tenantId,
                        persistStreamId,
                        turnError ?? "Chat turn aborted.",
                        cancellationToken: CancellationToken.None);
                }
                catch (Exception ex)
                {
                    logger.LogWarning(ex, "Failed to append terminal event for stream {StreamId}", persistStreamId);
                }
            }

            if (auditStarted && persistStreamId is not null)
            {
                var terminalStatus = turnError is null ? "done" : "error";
                await PublishGenerationAuditAsync(
                    tenantId,
                    persistStreamId,
                    answeredBy ?? pinnedAgentKey ?? "unknown",
                    audience,
                    kind,
                    terminalStatus,
                    inputSummaryJson,
                    turnError,
                    AgentGenerationAuditHash.Sha256(outputJson),
                    CancellationToken.None);
            }

            sw.Stop();
            PublishTurnEvent(tenantId, threadId, audience, answeredBy, toolsSeen, sw.ElapsedMilliseconds, empty, turnError);
            ChatTurnScope.Current = null;
        }
    }

    private async Task PublishGenerationAuditAsync(
        string tenantId,
        string processId,
        string agentKey,
        string audience,
        string kind,
        string status,
        string? inputSummaryJson,
        string? error,
        string? outputHash,
        CancellationToken cancellationToken)
    {
        try
        {
            await generationAudit.PublishAsync(
                new AgentGenerationAuditMessage(
                    tenantId,
                    processId,
                    agentKey,
                    audience,
                    kind,
                    status,
                    DateTimeOffset.UtcNow,
                    inputSummaryJson,
                    error,
                    outputHash),
                cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to publish generation audit for ProcessId={ProcessId}", processId);
        }
    }

    private async Task<AgentStreamEvent> EmitAsync(
        AgentStreamEvent evt,
        string tenantId,
        string? streamId,
        string threadId,
        CancellationToken cancellationToken)
    {
        if (streamId is not null)
        {
            try
            {
                await AgentStreamEventAppender.AppendIfNeededAsync(
                    eventStreams, tenantId, streamId, evt, threadId, cancellationToken);
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Event stream append failed for {StreamId} type {Type}", streamId, evt.Type);
            }
        }

        return evt;
    }
    private void PublishTurnEvent(
        string tenantId,
        string threadId,
        string audience,
        string? agentKey,
        IReadOnlyList<string> tools,
        long latencyMs,
        bool empty,
        string? error)
    {
        var evt = new ChatTurnEvent(
            EventType: "chat_turn",
            TenantId: tenantId,
            ThreadId: threadId,
            Audience: audience,
            AgentKey: agentKey,
            Tools: tools.Distinct(StringComparer.OrdinalIgnoreCase).ToArray(),
            LatencyMs: latencyMs,
            Empty: empty,
            Error: error,
            OccurredAt: DateTimeOffset.UtcNow);
        _ = chatEvents.PublishAsync(evt);
    }

    private async Task PersistTurnAsync(
        string tenantId,
        string audience,
        string threadId,
        IReadOnlyList<ChatThreadMessage> priorHistory,
        string userPrompt,
        string assistantText,
        CancellationToken cancellationToken)
    {
        var history = priorHistory.ToList();
        history.Add(new ChatThreadMessage("user", userPrompt));
        if (!string.IsNullOrWhiteSpace(assistantText))
            history.Add(new ChatThreadMessage("assistant", assistantText));

        await threadStore.SaveAsync(tenantId, audience, threadId, history, cancellationToken);
    }

    private async Task<List<AgentDefinition>> LoadAudienceAsync(
        string audience,
        string? agentKey,
        CancellationToken cancellationToken)
    {
        var system = await store.ListSystemAsync(cancellationToken);
        var list = system
            .Where(item =>
                string.Equals(item.Kind, "conversational", StringComparison.OrdinalIgnoreCase)
                && string.Equals(item.Audience, audience, StringComparison.OrdinalIgnoreCase)
                && !string.IsNullOrWhiteSpace(item.Description))
            .ToList();

        if (agentKey is null)
            return list;

        return list
            .Where(item => string.Equals(item.Key, agentKey, StringComparison.OrdinalIgnoreCase))
            .ToList();
    }
}
