namespace Commerce.Application.Features.CreateProduct.ApproveOnboarding;

public sealed record ApproveOnboardingResult(
    string WorkflowId,
    string? BrandId,
    string? CategoryId,
    string? ProductId);
