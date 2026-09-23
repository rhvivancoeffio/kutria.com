using Commerce.Application.Common.Abstracts;

namespace Commerce.Application.Features.Generative.StartProductImageGeneration;

public sealed record StartProductImageGenerationCommand(
    string Name,
    string? Description = null,
    int Count = 1) : ICommand<StartProductImageGenerationResult>;
