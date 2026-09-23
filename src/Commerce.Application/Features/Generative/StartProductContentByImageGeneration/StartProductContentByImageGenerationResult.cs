namespace Commerce.Application.Features.Generative.StartProductContentByImageGeneration;

public sealed record StartProductContentByImageGenerationResult(
    string ProcessId,
    string AgentKey,
    string Prompt,
    string Kind,
    string? ImageUrl,
    string? ImageAttachmentId);
