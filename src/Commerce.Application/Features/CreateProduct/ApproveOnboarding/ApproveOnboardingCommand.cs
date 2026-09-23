using Commerce.Application.Common.Abstracts;
using Commerce.Application.Features.CreateProduct;

namespace Commerce.Application.Features.CreateProduct.ApproveOnboarding;

public sealed record ApproveOnboardingCommand(
    string WorkflowId,
    string? TenantId,
    OnboardingApprovals Approvals)
    : ICommand<ApproveOnboardingResult>;
