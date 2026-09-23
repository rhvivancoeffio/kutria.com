using Commerce.Application.Abstracts;

namespace Commerce.Application.Features.CreateProduct;

public sealed record OnboardingDraftBrand(string Name, bool IsNew, string? Id = null);

public sealed record OnboardingDraftCategory(string Name, bool IsNew, string? Id = null);

public sealed record OnboardingDraftProduct(string Title, string? ImageUrl = null, string? Description = null);

public sealed record OnboardingDraft(
    OnboardingDraftBrand Brand,
    OnboardingDraftCategory Category,
    OnboardingDraftProduct Product);

public sealed record OnboardingApprovals(bool Brand, bool Category, bool Product);

public sealed record OnboardingWorkflowState(
    string WorkflowId,
    string TenantId,
    Guid IntegrationId,
    OnboardingDraft Draft,
    string Status,
    DateTimeOffset CreatedAtUtc);

public sealed record OnboardingCreateResult(
    string WorkflowId,
    string? BrandId,
    string? CategoryId,
    string? ProductId);

public interface IOnboardingWorkflowStore
{
    Task SaveAsync(OnboardingWorkflowState state, CancellationToken cancellationToken = default);

    Task<OnboardingWorkflowState?> GetAsync(
        string tenantId,
        string workflowId,
        CancellationToken cancellationToken = default);

    Task DeleteAsync(string tenantId, string workflowId, CancellationToken cancellationToken = default);
}

public interface IProductOnboardingCoordinator
{
    Task<OnboardingDraft> AnalyzeAsync(
        GravityStoreCredentials credentials,
        string? title,
        string? imageUrl,
        Stream? imageStream,
        string? imageContentType,
        CancellationToken cancellationToken = default);

    Task<OnboardingCreateResult> CreateApprovedAsync(
        GravityStoreCredentials credentials,
        string workflowId,
        OnboardingDraft draft,
        OnboardingApprovals approvals,
        CancellationToken cancellationToken = default);
}
