namespace Commerce.Application.Abstracts;

/// <summary>
/// Optional image attached to a chat turn. Agents discover it via tools / prompt context — not a hardcoded workflow.
/// </summary>
public sealed record ChatImageAttachment(
    string AttachmentId,
    byte[] Bytes,
    string ContentType,
    string Url);
