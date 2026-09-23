namespace Commerce.Infrastructure.Agents;

internal sealed class ToolCallTurn
{
    public static readonly AsyncLocal<ToolCallTurn?> Current = new();

    public required string TenantId { get; init; }
    public required string ThreadId { get; init; }
    public required string Audience { get; init; }

    public HashSet<string> Called { get; } = new(StringComparer.OrdinalIgnoreCase);

    public static ChatTurnState? ResolveTurn()
    {
        if (ChatTurnScope.Current is { } scope
            && !string.IsNullOrWhiteSpace(scope.TenantId)
            && !string.IsNullOrWhiteSpace(scope.ThreadId))
        {
            return scope;
        }

        var tool = Current.Value;
        if (tool is null
            || string.IsNullOrWhiteSpace(tool.TenantId)
            || string.IsNullOrWhiteSpace(tool.ThreadId))
        {
            return null;
        }

        return new ChatTurnState(tool.TenantId, tool.Audience, tool.ThreadId);
    }
}
