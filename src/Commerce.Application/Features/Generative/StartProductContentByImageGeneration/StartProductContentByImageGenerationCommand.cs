using Commerce.Application.Common.Abstracts;

namespace Commerce.Application.Features.Generative.StartProductContentByImageGeneration;

public sealed record StartProductContentByImageGenerationCommand(
    string? ImageUrl = null,
    string? ImageAttachmentId = null,
    string? Hint = null,
    string? BrandId = null,
    string? BrandName = null,
    string? CategoryId = null,
    string? CategoryPath = null) : ICommand<StartProductContentByImageGenerationResult>;
