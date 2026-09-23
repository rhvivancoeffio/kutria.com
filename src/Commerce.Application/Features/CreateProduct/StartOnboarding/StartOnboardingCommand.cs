using Commerce.Application.Common.Abstracts;

namespace Commerce.Application.Features.CreateProduct.StartOnboarding;

public sealed record StartOnboardingCommand(
    string? Title,
    string? ImageUrl,
    Stream? ImageStream,
    string? ImageContentType,
    Guid? IntegrationId = null,
    string? PublicBaseUrl = null)
    : ICommand<StartOnboardingResult>;
