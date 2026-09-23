namespace Commerce.Application.Features.Generative.StartProductImageGeneration;

public sealed record StartProductImageGenerationResult(
    string ProcessId,
    string AgentKey,
    string Prompt,
    string Kind);
