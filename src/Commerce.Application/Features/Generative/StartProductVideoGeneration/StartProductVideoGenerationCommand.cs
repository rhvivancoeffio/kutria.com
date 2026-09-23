using Commerce.Application.Common.Abstracts;

namespace Commerce.Application.Features.Generative.StartProductVideoGeneration;

public sealed record StartProductVideoGenerationCommand(
    string Name,
    string? Description = null,
    string? ImageUrl = null) : ICommand<StartProductVideoGenerationResult>;
