using System.Text.Json;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using Commerce.Application.Abstracts;
using Commerce.Infrastructure.Agents;

namespace Commerce.Infrastructure.Agents.Engines;

/// <summary>Shared helpers for buyer-switch and triage/handoff chat engines.</summary>
internal static class AgentChatSupport
{
    public const string TriageId = "triage";

    public static string BuildPrompt(string prompt, ChatImageAttachment? image)
    {
        if (image is null)
            return prompt;

        var note = $"[Attached image URL: {image.Url}]";
        return string.IsNullOrWhiteSpace(prompt)
            ? $"The user attached an image. {note} Use the appropriate tools for their intent (search similar products, or analyze and create catalog items)."
            : $"{prompt.Trim()}\n\n{note}";
    }

    public static ChatMessage BuildUserMessage(string prompt, ChatImageAttachment? image)
    {
        if (image is null || image.Bytes.Length == 0)
            return new ChatMessage(ChatRole.User, prompt);

        return new ChatMessage(ChatRole.User,
        [
            new TextContent(prompt),
            new DataContent(image.Bytes, image.ContentType)
        ]);
    }

    public static string Preview(string? text)
    {
        if (string.IsNullOrEmpty(text))
            return "(empty)";

        var compact = text.Replace('\n', ' ').Replace('\r', ' ').Trim();
        return compact.Length <= 180 ? compact : compact[..180];
    }

    public static AgentDefinition ResolveAgent(IReadOnlyList<AgentDefinition> catalog, string executorId)
    {
        var normalized = executorId.Replace('_', '-');
        return catalog.FirstOrDefault(agent =>
            string.Equals(agent.Key, executorId, StringComparison.OrdinalIgnoreCase)
            || normalized.Contains(agent.Key, StringComparison.OrdinalIgnoreCase))
            ?? catalog[0];
    }

    public static string? Forbidden(string json, string? reply, IReadOnlyList<string>? phrases)
        => (phrases ?? []).FirstOrDefault(phrase =>
            !string.IsNullOrWhiteSpace(phrase)
            && (json.Contains(phrase, StringComparison.OrdinalIgnoreCase)
                || (reply?.Contains(phrase, StringComparison.OrdinalIgnoreCase) ?? false)));

    public static AIAgent CreateSpecialist(
        IChatClient chat,
        AgentDefinition definition,
        string tenantId,
        string threadId,
        string audience,
        IServiceProvider services,
        ChatHistoryProvider chatHistoryProvider,
        IReadOnlyList<AIContextProvider> turnContextProviders,
        IAgentModule? module)
    {
        // IAsyncEnumerable yield return drops AsyncLocal; pin turn identity for CreateTools capture.
        var previousScope = ChatTurnScope.Current;
        var previousTurn = ToolCallTurn.Current.Value;
        ChatTurnScope.Current = new ChatTurnState(tenantId, audience, threadId);
        ToolCallTurn.Current.Value = new ToolCallTurn
        {
            TenantId = tenantId,
            ThreadId = threadId,
            Audience = audience
        };
        try
        {
            using var schema = JsonDocument.Parse(definition.OutputSchema);
            return chat.AsAIAgent(new ChatClientAgentOptions
            {
                Id = definition.Key,
                Name = definition.Name,
                Description = definition.Description,
                ChatHistoryProvider = chatHistoryProvider,
                AIContextProviders = turnContextProviders.ToArray(),
                ChatOptions = new ChatOptions
                {
                    Instructions = definition.Instructions,
                    ModelId = definition.Model,
                    Temperature = definition.Temperature,
                    MaxOutputTokens = definition.MaxOutputTokens,
                    ResponseFormat = ChatResponseFormat.ForJsonSchema(
                        schema.RootElement.Clone(),
                        definition.Key,
                        definition.Name),
                    Tools = module?.CreateTools(definition, tenantId, services).ToList() ?? []
                }
            });
        }
        finally
        {
            ChatTurnScope.Current = previousScope;
            ToolCallTurn.Current.Value = previousTurn;
        }
    }

    public static AIAgent CreateClassifier(IChatClient chat, AgentDefinition definition)
    {
        using var schema = JsonDocument.Parse(
            string.IsNullOrWhiteSpace(definition.OutputSchema)
                ? """{"type":"object","properties":{}}"""
                : definition.OutputSchema);

        return chat.AsAIAgent(new ChatClientAgentOptions
        {
            Id = definition.Key,
            Name = definition.Name,
            Description = definition.Description,
            ChatOptions = new ChatOptions
            {
                Instructions = definition.Instructions,
                ModelId = definition.Model,
                Temperature = definition.Temperature ?? 0,
                MaxOutputTokens = definition.MaxOutputTokens,
                ResponseFormat = ChatResponseFormat.ForJsonSchema(
                    schema.RootElement.Clone(),
                    definition.Key,
                    definition.Name)
            }
        });
    }
}
