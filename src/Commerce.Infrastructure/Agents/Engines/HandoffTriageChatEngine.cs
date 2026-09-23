using System.Text;
using Microsoft.Agents.AI;
using Microsoft.Agents.AI.Workflows;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;
using Commerce.Application.Abstracts;
using Commerce.Infrastructure.Agents;
using Commerce.Infrastructure.Agents.Events;
using Commerce.Infrastructure.Agents.History;
using Commerce.Infrastructure.Agents.Profile;

namespace Commerce.Infrastructure.Agents.Engines;

/// <summary>Runtime A: MAF triage handoff for non-buyer audiences.</summary>
internal sealed class HandoffTriageChatEngine(
    IEnumerable<IAgentModule> modules,
    IServiceProvider services,
    IChatThreadStore threadStore,
    ChatHistoryProvider chatHistoryProvider,
    SessionBuyerProfileAIContextProvider sessionBuyerProfileProvider,
    ChatEventsAIContextProvider chatEventsProvider,
    ILogger<HandoffTriageChatEngine> logger) : IChatTurnEngine
{
    private AIContextProvider[] TurnContextProviders => [sessionBuyerProfileProvider, chatEventsProvider];

    public bool CanHandle(string audience)
        => !string.Equals(audience, "buyer", StringComparison.OrdinalIgnoreCase);

    public async IAsyncEnumerable<AgentStreamEvent> StreamAsync(
        IChatClient chat,
        IReadOnlyList<AgentDefinition> catalog,
        string prompt,
        string tenantId,
        string threadId,
        string audience,
        ChatImageAttachment? image,
        IReadOnlyList<ChatThreadMessage> history,
        [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken)
    {
        StreamingRun? run = null;
        string? startError = null;
        var canceled = false;
        yield return AgentStreamEvent.Status("Enrutando tu consulta…");
        try
        {
            run = await StartHandoffAsync(
                chat, catalog, prompt, history, threadId, tenantId, audience, image, cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            canceled = true;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Chat handoff failed to start for thread {ThreadId}", threadId);
            startError = ex.Message;
        }

        if (canceled)
        {
            yield return AgentStreamEvent.Error("Chat turn canceled.");
            yield break;
        }

        if (startError is not null || run is null)
        {
            if (run is not null)
                await run.DisposeAsync();

            yield return AgentStreamEvent.Error(startError ?? "Agent failed to start.");
            yield break;
        }

        var buffers = new Dictionary<string, StringBuilder>(StringComparer.OrdinalIgnoreCase);
        var order = new List<string>();
        var completed = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var handoffs = new List<string>();
        var mafNotes = new List<string>();
        var triageTrace = new TriageTrace();
        var specialistKeys = catalog.Select(agent => agent.Key).ToArray();
        logger.LogInformation(
            "Chat turn {ThreadId} specialist map {Map}",
            threadId,
            string.Join(", ", specialistKeys.Select((key, index) => $"{index}={key}")));

        try
        {
            await foreach (var evt in run.WatchStreamAsync(cancellationToken))
            {
                if (evt is AgentResponseUpdateEvent update)
                {
                    var toolNames = ToolNames(update).ToArray();
                    var contentKinds = DescribeContents(update.Update.Contents);
                    foreach (var handoff in toolNames.Where(IsHandoffTool))
                    {
                        var target = ResolveHandoffTarget(handoff, specialistKeys);
                        handoffs.Add(target);
                        logger.LogInformation(
                            "Chat triage handed off via {Tool} to {Target} on turn {ThreadId}",
                            handoff,
                            target,
                            threadId);
                    }

                    var text = update.Update.Text;
                    var isTriage = IsTriage(update.ExecutorId);
                    if (isTriage)
                    {
                        triageTrace.Record(toolNames, text, contentKinds);
                        if (toolNames.Length > 0 || !string.IsNullOrWhiteSpace(text) || contentKinds.Contains("fn", StringComparison.Ordinal))
                        {
                            logger.LogInformation(
                                "Chat triage update tools [{Tools}] contents [{Contents}] text {Text}",
                                string.Join(", ", toolNames),
                                contentKinds,
                                AgentChatSupport.Preview(text));
                        }
                    }
                    else if (toolNames.Length > 0 || !string.IsNullOrWhiteSpace(text))
                    {
                        logger.LogInformation(
                            "Chat update executor {ExecutorId} tools [{Tools}] contents [{Contents}] text {Text}",
                            update.ExecutorId,
                            string.Join(", ", toolNames),
                            contentKinds,
                            AgentChatSupport.Preview(text));
                    }

                    CaptureUpdate(update, completed);
                    foreach (var name in toolNames.Where(name => !IsHandoffTool(name)))
                        yield return AgentStreamEvent.Tool(name);

                    continue;
                }

                if (evt is AgentResponseEvent response)
                {
                    if (IsTriage(response.ExecutorId))
                    {
                        triageTrace.RecordResponse(response.Response?.Text);
                        logger.LogInformation(
                            "Chat triage response text {Text}",
                            AgentChatSupport.Preview(response.Response?.Text));
                    }
                    else if (!string.IsNullOrWhiteSpace(response.Response?.Text))
                    {
                        logger.LogInformation(
                            "Chat response executor {ExecutorId} text {Text}",
                            response.ExecutorId,
                            AgentChatSupport.Preview(response.Response?.Text));
                    }

                    Capture(response.ExecutorId, response.Response?.Text, buffers, order, replace: true);
                    if (!string.IsNullOrWhiteSpace(response.ExecutorId))
                        completed.Add(response.ExecutorId);

                    continue;
                }

                if (evt is ExecutorFailedEvent failed)
                {
                    var ex = failed.Data as Exception;
                    logger.LogError(
                        ex,
                        "Chat MAF executor failed. ThreadId={ThreadId} ExecutorId={ExecutorId}",
                        threadId,
                        failed.ExecutorId);
                    mafNotes.Add($"executor_failed:{failed.ExecutorId}:{ex?.GetType().Name}:{AgentChatSupport.Preview(ex?.Message)}");
                    continue;
                }

                if (evt is WorkflowErrorEvent workflowError)
                {
                    logger.LogError(
                        workflowError.Exception,
                        "Chat MAF workflow error. ThreadId={ThreadId}",
                        threadId);
                    mafNotes.Add(
                        $"workflow_error:{workflowError.Exception?.GetType().Name}:{AgentChatSupport.Preview(workflowError.Exception?.Message)}");
                    continue;
                }

                if (evt is WorkflowWarningEvent workflowWarning)
                {
                    logger.LogWarning(
                        "Chat MAF workflow warning. ThreadId={ThreadId} Data={Data}",
                        threadId,
                        AgentChatSupport.Preview(workflowWarning.Data?.ToString()));
                    mafNotes.Add($"workflow_warning:{AgentChatSupport.Preview(workflowWarning.Data?.ToString())}");
                    continue;
                }

                if (evt is RequestInfoEvent requestInfo)
                {
                    var port = requestInfo.Request?.PortInfo?.ToString() ?? "(unknown-port)";
                    logger.LogWarning(
                        "Chat MAF external request (run may be waiting). ThreadId={ThreadId} Port={Port} RequestId={RequestId}",
                        threadId,
                        port,
                        requestInfo.Request?.RequestId);
                    mafNotes.Add($"external_request:{port}:{requestInfo.Request?.RequestId}");
                    continue;
                }

                if (evt is ExecutorInvokedEvent invoked)
                {
                    logger.LogInformation(
                        "Chat MAF executor invoked. ThreadId={ThreadId} ExecutorId={ExecutorId}",
                        threadId,
                        invoked.ExecutorId);
                    continue;
                }

                if (evt is ExecutorCompletedEvent completedEvt)
                {
                    logger.LogInformation(
                        "Chat MAF executor completed. ThreadId={ThreadId} ExecutorId={ExecutorId}",
                        threadId,
                        completedEvt.ExecutorId);
                    continue;
                }

                logger.LogInformation(
                    "Chat MAF event {EventType} executor/data {Detail} on turn {ThreadId}",
                    evt.GetType().Name,
                    DescribeWorkflowEvent(evt),
                    threadId);
            }
        }
        finally
        {
            await run.DisposeAsync();
        }

        var agentKey = catalog[0].Key;
        string json = string.Empty;
        string? reply = null;
        string? parseError = null;
        var parsed = false;
        for (var i = order.Count - 1; i >= 0; i--)
        {
            if (!AgentOutput.TryRead(buffers[order[i]].ToString(), out json, out reply, out parseError))
                continue;

            agentKey = order[i];
            parsed = true;
            break;
        }

        var definition = AgentChatSupport.ResolveAgent(catalog, agentKey);

        if (!parsed)
        {
            var captured = order.Count == 0
                ? "(none — triage never got a specialist reply)"
                : string.Join(" | ", order.Select(id => $"{id}: {AgentChatSupport.Preview(buffers[id].ToString())}"));
            var triageSummary = triageTrace.Summarize();
            var mafSummary = mafNotes.Count == 0 ? "(none)" : string.Join(" | ", mafNotes);
            logger.LogWarning(
                "Chat turn {ThreadId} produced no JSON. Handoffs [{Handoffs}]. Specialists that wrote [{Writers}]. Captured {Captured}. ParseError {Error}. Triage {Triage}. MafNotes {MafNotes}",
                threadId,
                handoffs.Count == 0 ? "(none)" : string.Join(", ", handoffs),
                order.Count == 0 ? "(none)" : string.Join(", ", order),
                captured,
                parseError ?? "(null)",
                triageSummary,
                mafSummary);
            yield return AgentStreamEvent.Error(
                BuildEmptyTurnError(parseError, handoffs, order.Count, triageSummary, mafSummary));
            yield break;
        }

        logger.LogInformation("Chat turn {ThreadId} answered by {AgentKey}", threadId, definition.Key);

        var blocked = AgentChatSupport.Forbidden(json, reply, definition.ForbiddenPhrases);
        if (blocked is not null)
        {
            yield return AgentStreamEvent.Error($"Agent output used a forbidden phrase: {blocked}.");
            yield break;
        }

        await PersistTurnAsync(tenantId, audience, threadId, history, prompt, json, cancellationToken)
            .ConfigureAwait(false);

        if (!string.IsNullOrEmpty(reply))
            yield return AgentStreamEvent.Token(reply);

        yield return AgentStreamEvent.Output(json);
        yield return AgentStreamEvent.Done(definition.Key, threadId);
    }

    private async Task<StreamingRun> StartHandoffAsync(
        IChatClient chat,
        IReadOnlyList<AgentDefinition> catalog,
        string prompt,
        IReadOnlyList<ChatThreadMessage> history,
        string threadId,
        string tenantId,
        string audience,
        ChatImageAttachment? image,
        CancellationToken cancellationToken)
    {
        var triage = chat.AsAIAgent(new ChatClientAgentOptions
        {
            Id = AgentChatSupport.TriageId,
            Name = "Triage",
            Description = "Enruta al especialista. No responde al cliente.",
            ChatHistoryProvider = chatHistoryProvider,
            AIContextProviders = TurnContextProviders,
            ChatOptions = new ChatOptions
            {
                Instructions = "No respondas al cliente. Elige un especialista y transfiere. Usa el estado de sesión si está presente. Transfiere según las descripciones de los especialistas. Si saluda o no hay intención, usa el que ayuda a encontrar productos. Si hay foto y pide crear/alta de producto, usa el gestor de productos. Si hay foto y quiere buscar/encontrar similares, usa discovery.",
                ToolMode = ChatToolMode.RequireAny,
                Temperature = 0
            }
        });

        var specialists = catalog
            .Select(definition =>
            {
                var module = modules.FirstOrDefault(m =>
                    string.Equals(m.Key, definition.Key, StringComparison.OrdinalIgnoreCase));
                return AgentChatSupport.CreateSpecialist(
                    chat, definition, tenantId, threadId, audience, services, chatHistoryProvider, TurnContextProviders, module);
            })
            .ToList();

        var workflow = AgentWorkflowBuilder
            .CreateHandoffBuilderWith(triage)
            .WithHandoffs(triage, specialists)
            .WithHandoffInstructions(
                $"""
                Nunca respondas al usuario. Transfiere siempre llamando exactamente una función cuyo nombre empieza por {HandoffWorkflowBuilder.FunctionPrefix}. La descripción de cada función indica el especialista. Usa el estado de sesión si está presente; no inventes carrito ni producto. Si el mensaje es un saludo o la intención no es clara, transfiere al especialista que ayuda a encontrar productos. Si hay imagen adjunta y pide crear producto, marca o categoría, transfiere al gestor de productos. Si hay imagen y busca productos similares, transfiere a discovery. No narres la transferencia.
                """)
            .EmitAgentResponseEvents(true)
            .EmitAgentResponseUpdateEvents(true)
            .Build();

        var messages = history.Select(RedisChatHistoryProvider.ToChatMessage).ToList();
        messages.Add(AgentChatSupport.BuildUserMessage(prompt, image));
        var run = await InProcessExecution.RunStreamingAsync(workflow, messages, threadId, cancellationToken);
        await run.TrySendMessageAsync(new TurnToken(true));
        return run;
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

    private static void CaptureUpdate(AgentResponseUpdateEvent update, HashSet<string> completed)
    {
        _ = ToolNames(update).ToArray();
        _ = IsTriage(update.ExecutorId) || completed.Contains(update.ExecutorId ?? string.Empty);
        // Do not append streaming text. Partial JSON chunks break AgentOutput.TryRead.
        // Final specialist text is captured from AgentResponseEvent (replace: true).
    }

    private static void Capture(
        string? executorId,
        string? text,
        Dictionary<string, StringBuilder> buffers,
        List<string> order,
        bool replace)
    {
        if (IsTriage(executorId) || string.IsNullOrEmpty(text))
            return;

        var id = executorId ?? "specialist";
        if (replace)
            buffers[id] = new StringBuilder(text);
        else if (buffers.TryGetValue(id, out var buffer))
            buffer.Append(text);
        else
            buffers[id] = new StringBuilder(text);

        if (!order.Contains(id))
            order.Add(id);
    }

    private static bool IsHandoffTool(string name)
        => name.StartsWith(HandoffWorkflowBuilder.FunctionPrefix, StringComparison.OrdinalIgnoreCase);

    private static string ResolveHandoffTarget(string toolName, IReadOnlyList<string> specialistKeys)
    {
        var suffix = toolName[HandoffWorkflowBuilder.FunctionPrefix.Length..].Trim('_', '-', ' ');
        if (int.TryParse(suffix, out var index))
        {
            var asZero = index >= 0 && index < specialistKeys.Count ? $"{index}:{specialistKeys[index]}" : null;
            var asOne = index >= 1 && index <= specialistKeys.Count ? $"{index}:{specialistKeys[index - 1]}" : null;
            if (asZero is not null && asOne is not null && !string.Equals(asZero, asOne, StringComparison.Ordinal))
                return $"{toolName} → 0-based {asZero} | 1-based {asOne}";

            return asZero ?? asOne ?? $"out-of-range({toolName})";
        }

        var byKey = specialistKeys.FirstOrDefault(key =>
            suffix.Contains(key, StringComparison.OrdinalIgnoreCase)
            || key.Contains(suffix, StringComparison.OrdinalIgnoreCase));
        return byKey is null ? $"unknown({toolName})" : byKey;
    }

    private static bool IsTriage(string? executorId)
        => !string.IsNullOrWhiteSpace(executorId)
           && (string.Equals(executorId, AgentChatSupport.TriageId, StringComparison.OrdinalIgnoreCase)
               || executorId.Contains("triage", StringComparison.OrdinalIgnoreCase));

    private static IEnumerable<string> ToolNames(AgentResponseUpdateEvent update)
    {
        if (update.Update.Contents is null)
            yield break;

        foreach (var content in update.Update.Contents)
        {
            if (content is FunctionCallContent call && !string.IsNullOrWhiteSpace(call.Name))
                yield return call.Name;
        }
    }

    private static string DescribeContents(IList<AIContent>? contents)
    {
        if (contents is null || contents.Count == 0)
            return "(none)";

        return string.Join(", ", contents.Select(content => content switch
        {
            FunctionCallContent call => $"fn:{call.Name}",
            FunctionResultContent => "fn_result",
            TextContent => "text",
            DataContent => "data",
            UriContent => "uri",
            _ => content.GetType().Name
        }));
    }

    private static string DescribeWorkflowEvent(WorkflowEvent evt)
    {
        if (evt is ExecutorEvent executor)
            return $"ExecutorId={executor.ExecutorId}; Data={AgentChatSupport.Preview(executor.Data?.ToString())}";

        if (evt is WorkflowOutputEvent output)
            return $"ExecutorId={output.ExecutorId}; DataType={output.Data?.GetType().Name}";

        return $"DataType={evt.Data?.GetType().Name ?? evt.GetType().Name}";
    }

    private static string BuildEmptyTurnError(
        string? parseError,
        IReadOnlyList<string> handoffs,
        int specialistWriterCount,
        string triageSummary,
        string mafSummary)
    {
        if (!string.IsNullOrWhiteSpace(parseError))
            return parseError;

        if (handoffs.Count == 0 && specialistWriterCount == 0)
        {
            return "Triage did not hand off to a specialist. "
                + $"Triage={triageSummary}. Maf={mafSummary}";
        }

        if (specialistWriterCount == 0)
        {
            return "Handoff occurred but no specialist produced output. "
                + $"Handoffs=[{string.Join(", ", handoffs)}]. Maf={mafSummary}";
        }

        return "Agent returned an empty or non-JSON response. "
            + $"Parse/Maf={mafSummary}";
    }

    private sealed class TriageTrace
    {
        private readonly List<string> _tools = [];
        private readonly List<string> _contents = [];
        private string? _text;
        private bool _sawUpdate;

        public void Record(IReadOnlyList<string> tools, string? text, string contents)
        {
            _sawUpdate = true;
            foreach (var tool in tools)
            {
                if (!_tools.Contains(tool, StringComparer.OrdinalIgnoreCase))
                    _tools.Add(tool);
            }

            if (!string.IsNullOrWhiteSpace(contents) && contents != "(none)")
                _contents.Add(contents);

            if (!string.IsNullOrWhiteSpace(text))
                _text = text;
        }

        public void RecordResponse(string? text)
        {
            _sawUpdate = true;
            if (!string.IsNullOrWhiteSpace(text))
                _text = text;
        }

        public string Summarize()
        {
            if (!_sawUpdate)
                return "(no triage events)";

            var tools = _tools.Count == 0 ? "(none)" : string.Join(", ", _tools);
            var contents = _contents.Count == 0 ? "(none)" : string.Join(" | ", _contents.TakeLast(3));
            return $"tools=[{tools}] contents=[{contents}] text={AgentChatSupport.Preview(_text)}";
        }
    }
}
