namespace Commerce.Application.Features.Generative.StartProductVideoGeneration;

public sealed record StartProductVideoGenerationResult(
    string ProcessId,
    string AgentKey,
    string Prompt,
    string Kind);
