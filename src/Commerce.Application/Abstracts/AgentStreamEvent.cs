namespace Commerce.Application.Abstracts;

public sealed record AgentStreamEvent(string Type, string? Text, string? ToolName, string? AgentKey, string? ThreadId)
{
    public static AgentStreamEvent Token(string text) => new("token", text, null, null, null);
    public static AgentStreamEvent Output(string json) => new("output", json, null, null, null);
    public static AgentStreamEvent Tool(string toolName) => new("tool", null, toolName, null, null);
    /// <summary>Human-readable progress for the chat UI while the agent is working.</summary>
    public static AgentStreamEvent Status(string text) => new("status", text, null, null, null);
    public static AgentStreamEvent Done(string agentKey, string threadId) => new("done", null, null, agentKey, threadId);
    public static AgentStreamEvent Error(string text) => new("error", text, null, null, null);
}
