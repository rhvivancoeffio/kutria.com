using Commerce.Application.Common.Abstracts;

namespace Commerce.Application.Features.Generative.StartProductContentGeneration;

public sealed record StartProductContentGenerationCommand(
    string Name,
    IReadOnlyList<string>? Modes = null,
    string? BrandId = null,
    string? BrandName = null,
    string? CategoryId = null,
    string? CategoryPath = null) : ICommand<StartProductContentGenerationResult>;
