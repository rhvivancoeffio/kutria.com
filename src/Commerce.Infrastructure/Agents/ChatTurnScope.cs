namespace Commerce.Infrastructure.Agents;

internal sealed record ChatTurnState(
    string TenantId,
    string Audience,
    string ThreadId,
    string? ImageAttachmentId = null,
    string? ImageUrl = null,
    string? ImageContentType = null);

/// <summary>
/// Ambient turn identity for MAF ChatHistoryProvider / AIContextProvider (singleton, no per-session fields).
/// </summary>
internal static class ChatTurnScope
{
    private static readonly AsyncLocal<ChatTurnState?> CurrentState = new();

    public static ChatTurnState? Current
    {
        get => CurrentState.Value;
        set => CurrentState.Value = value;
    }
}
