using Commerce.Application.Features.CreateProduct;

namespace Commerce.Application.Features.CreateProduct.StartOnboarding;

public sealed record StartOnboardingResult(
    string WorkflowId,
    string TenantId,
    OnboardingDraft Draft);
