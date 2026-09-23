namespace Commerce.Application.Features.Generative.StartProductContentGeneration;

public sealed record StartProductContentGenerationResult(
    string ProcessId,
    string AgentKey,
    string Prompt,
    string Kind);
