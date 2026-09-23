using System.Threading.Channels;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Commerce.Application.Abstracts;
using Commerce.Infrastructure.Agents;
using Commerce.Infrastructure.Agents.Events;
using Commerce.Infrastructure.Agents.Definitions.BuyerSwitch;
using Commerce.Infrastructure.Agents.Profile;

namespace Commerce.Infrastructure.Agents.Engines;

/// <summary>Runtime B: YAML intent-classifier + deterministic route + specialist RunAsync.</summary>
internal sealed class BuyerSwitchChatEngine(
    IAgentDefinitionStore store,
    IEnumerable<IAgentModule> modules,
    IServiceProvider services,
    IChatThreadStore threadStore,
    ChatHistoryProvider chatHistoryProvider,
    SessionBuyerProfileAIContextProvider sessionBuyerProfileProvider,
    ChatEventsAIContextProvider chatEventsProvider,
    ILogger<BuyerSwitchChatEngine> logger) : IChatTurnEngine
{
    private AIContextProvider[] TurnContextProviders => [sessionBuyerProfileProvider, chatEventsProvider];

    public bool CanHandle(string audience)
        => string.Equals(audience, "buyer", StringComparison.OrdinalIgnoreCase);

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
        yield return AgentStreamEvent.Status("Entendiendo tu mensaje…");
        yield return AgentStreamEvent.Tool(BuyerSwitchWorkflow.ClassifierId);

        var channel = Channel.CreateUnbounded<AgentStreamEvent>(new UnboundedChannelOptions
        {
            SingleReader = true,
            SingleWriter = true
        });

        BuyerSwitchTurnResult? result = null;
        string? error = null;
        var canceled = false;
        string phase = "run";

        var runTask = Task.Run(async () =>
        {
            try
            {
                result = await RunAsync(
                        chat,
                        catalog,
                        prompt,
                        threadId,
                        tenantId,
                        image,
                        cancellationToken,
                        p => phase = p,
                        status => channel.Writer.TryWrite(AgentStreamEvent.Status(status)))
                    .ConfigureAwait(false);
            }
            catch (OperationCanceledException ex) when (cancellationToken.IsCancellationRequested)
            {
                logger.LogWarning(
                    "Chat buyer-switch canceled by RequestAborted/SSE token. ThreadId={ThreadId} Phase={Phase} ExType={ExType} Message={Message}",
                    threadId,
                    phase,
                    ex.GetType().Name,
                    ex.Message);
                canceled = true;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Chat buyer-switch failed for thread {ThreadId} phase {Phase}", threadId, phase);
                error = ex.Message;
            }
            finally
            {
                channel.Writer.TryComplete();
            }
        }, CancellationToken.None);

        await foreach (var evt in channel.Reader.ReadAllAsync(cancellationToken).ConfigureAwait(false))
            yield return evt;

        await runTask.ConfigureAwait(false);

        if (canceled)
        {
            yield return AgentStreamEvent.Error($"Chat turn canceled during {phase} (client/SSE RequestAborted).");
            yield break;
        }

        if (error is not null || result is null)
        {
            yield return AgentStreamEvent.Error(error ?? "Buyer switch failed.");
            yield break;
        }

        foreach (var name in result.ToolNames)
        {
            yield return AgentStreamEvent.Status(DescribeTool(name));
            yield return AgentStreamEvent.Tool(name);
        }

        if (!AgentOutput.TryRead(result.ResponseText, out var json, out var reply, out var parseError))
        {
            logger.LogWarning(
                "Chat turn {ThreadId} buyer specialist {AgentKey} invalid JSON: {Error}. Preview {Preview}",
                threadId,
                result.AgentKey,
                parseError,
                AgentChatSupport.Preview(result.ResponseText));
            yield return AgentStreamEvent.Error(parseError ?? "Buyer specialist returned invalid JSON.");
            yield break;
        }

        json = AgentOutput.EnsureDisplayMessage(json, out reply);

        var definition = AgentChatSupport.ResolveAgent(catalog, result.AgentKey);
        var blocked = AgentChatSupport.Forbidden(json, reply, definition.ForbiddenPhrases);
        if (blocked is not null)
        {
            yield return AgentStreamEvent.Error($"Agent output used a forbidden phrase: {blocked}.");
            yield break;
        }

        yield return AgentStreamEvent.Status("Preparando respuesta…");

        await PersistTurnAsync(tenantId, audience, threadId, history, prompt, json, cancellationToken)
            .ConfigureAwait(false);

        if (!string.IsNullOrEmpty(reply))
            yield return AgentStreamEvent.Token(reply);
        yield return AgentStreamEvent.Output(json);
        yield return AgentStreamEvent.Done(definition.Key, threadId);
    }

    private async Task<BuyerSwitchTurnResult> RunAsync(
        IChatClient chat,
        IReadOnlyList<AgentDefinition> catalog,
        string prompt,
        string threadId,
        string tenantId,
        ChatImageAttachment? image,
        CancellationToken cancellationToken,
        Action<string> setPhase,
        Action<string> onStatus)
    {
        // Task.Run does not inherit AsyncLocal from the SSE enumerator — pin turn identity here.
        var previousScope = ChatTurnScope.Current;
        var previousTurn = ToolCallTurn.Current.Value;
        ChatTurnScope.Current = new ChatTurnState(tenantId, "buyer", threadId);
        ToolCallTurn.Current.Value = new ToolCallTurn
        {
            TenantId = tenantId,
            ThreadId = threadId,
            Audience = "buyer"
        };
        try
        {
            return await RunCoreAsync(
                    chat, catalog, prompt, threadId, tenantId, image, cancellationToken, setPhase, onStatus)
                .ConfigureAwait(false);
        }
        finally
        {
            ChatTurnScope.Current = previousScope;
            ToolCallTurn.Current.Value = previousTurn;
        }
    }

    private async Task<BuyerSwitchTurnResult> RunCoreAsync(
        IChatClient chat,
        IReadOnlyList<AgentDefinition> catalog,
        string prompt,
        string threadId,
        string tenantId,
        ChatImageAttachment? image,
        CancellationToken cancellationToken,
        Action<string> setPhase,
        Action<string> onStatus)
    {
        setPhase("load-classifier");
        onStatus("Preparando el asistente…");
        var classifierDef = await store.GetAsync(tenantId, BuyerSwitchWorkflow.ClassifierId, cancellationToken)
                .ConfigureAwait(false)
            ?? throw new InvalidOperationException(
                $"Missing agent definition '{BuyerSwitchWorkflow.ClassifierId}'. Add data/agents/intent-classifier/.");

        if (!string.Equals(classifierDef.Kind, "router", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(
                $"Agent '{BuyerSwitchWorkflow.ClassifierId}' must have kind=router (found '{classifierDef.Kind}').");
        }

        setPhase("load-profile");
        var profileStore = services.GetRequiredService<ISessionBuyerProfileStore>();
        var profile = await profileStore.GetAsync(tenantId, threadId, cancellationToken).ConfigureAwait(false);

        logger.LogInformation(
            "Chat turn {ThreadId} runtime buyer-switch classifier={Classifier} model={Model} cartItems={CartItems}",
            threadId,
            classifierDef.Key,
            classifierDef.Model ?? "(default)",
            profile.CartItemCount);

        setPhase("classify");
        onStatus("Analizando tu intención…");
        var userText = AgentChatSupport.BuildPrompt(prompt, image);
        var intent = await BuyerSwitchWorkflow.ClassifyAsync(
                AgentChatSupport.CreateClassifier(chat, classifierDef),
                profile,
                userText,
                logger,
                cancellationToken)
            .ConfigureAwait(false);

        var agentKey = BuyerSwitchWorkflow.ResolveSpecialistKey(intent, catalog);
        var definition = AgentChatSupport.ResolveAgent(catalog, agentKey);
        logger.LogInformation(
            "Chat turn {ThreadId} buyer-switch routed to {AgentKey} intent {Intent}",
            threadId,
            definition.Key,
            intent.Intent);

        onStatus(DescribeIntent(intent.Intent, definition.Name));
        setPhase($"specialist:{definition.Key}");
        var module = modules.FirstOrDefault(m => string.Equals(m.Key, definition.Key, StringComparison.OrdinalIgnoreCase));
        var specialist = AgentChatSupport.CreateSpecialist(
            chat, definition, tenantId, threadId, "buyer", services, chatHistoryProvider, TurnContextProviders, module);

        onStatus("Consultando herramientas…");
        var response = await specialist.RunAsync(
                new ChatMessage(ChatRole.User, BuyerSwitchWorkflow.BuildSpecialistPrompt(intent)),
                cancellationToken: cancellationToken)
            .ConfigureAwait(false);

        var toolNames = response.Messages
            .SelectMany(m => m.Contents ?? [])
            .OfType<FunctionCallContent>()
            .Select(c => c.Name)
            .Where(n => !string.IsNullOrWhiteSpace(n))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();

        logger.LogInformation(
            "Chat turn {ThreadId} buyer specialist {AgentKey} tools [{Tools}] text {Text}",
            threadId,
            definition.Key,
            toolNames.Length == 0 ? "(none)" : string.Join(", ", toolNames),
            AgentChatSupport.Preview(response.Text));

        return new BuyerSwitchTurnResult(definition.Key, response.Text ?? string.Empty, toolNames);
    }

    private static string DescribeIntent(BuyerIntentKind intent, string agentName) => intent switch
    {
        BuyerIntentKind.Search => "Buscando productos…",
        BuyerIntentKind.AddCart => "Agregando al carrito…",
        BuyerIntentKind.UpdateCart => "Actualizando el carrito…",
        BuyerIntentKind.RemoveItem => "Quitando ítems del carrito…",
        BuyerIntentKind.Checkout => "Preparando el checkout…",
        BuyerIntentKind.PostSales => "Revisando tu pedido…",
        BuyerIntentKind.Faq => "Buscando información…",
        _ => $"Consultando {agentName}…"
    };

    private static string DescribeTool(string toolName) => toolName.ToLowerInvariant() switch
    {
        "search_products" => "Buscando en el catálogo…",
        "compare_products" => "Comparando productos…",
        "get_product_details" => "Cargando detalle del producto…",
        "get_cart" => "Revisando tu carrito…",
        "create_cart" => "Creando el carrito…",
        "add_item" => "Agregando al carrito…",
        "update_item_qty" => "Actualizando cantidades…",
        "remove_item" => "Quitando del carrito…",
        "apply_coupon" => "Aplicando cupón…",
        "remove_coupon" => "Quitando cupón…",
        "cart_checkout" or "create_order_draft" => "Generando checkout…",
        "intent-classifier" => "Analizando tu intención…",
        _ => $"Ejecutando {toolName}…"
    };

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

    private sealed record BuyerSwitchTurnResult(
        string AgentKey,
        string ResponseText,
        IReadOnlyList<string> ToolNames);
}
