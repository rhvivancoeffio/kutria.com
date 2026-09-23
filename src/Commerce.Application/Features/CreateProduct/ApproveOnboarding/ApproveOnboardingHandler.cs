using Finbuckle.MultiTenant.Abstractions;
using Microsoft.Extensions.Logging;
using Commerce.Application.Abstracts;
using Commerce.Application.Common;
using Commerce.Application.Common.Abstracts;
using Commerce.Application.Features.CreateProduct;
using Commerce.Domain.Tenants;

namespace Commerce.Application.Features.CreateProduct.ApproveOnboarding;

public sealed class ApproveOnboardingHandler(
    ICommerceDbContext db,
    IWorkspaceContext workspaceContext,
    IProductOnboardingCoordinator coordinator,
    IOnboardingWorkflowStore workflowStore,
    IMultiTenantContextAccessor<CommerceTenantInfo> tenantAccessor,
    ILogger<ApproveOnboardingHandler> logger)
    : ICommandHandler<ApproveOnboardingCommand, ApproveOnboardingResult>
{
    public async Task<ApproveOnboardingResult> Handle(ApproveOnboardingCommand request, CancellationToken cancellationToken)
    {
        var tenant = tenantAccessor.MultiTenantContext?.TenantInfo
            ?? throw new InvalidOperationException("Tenant is required.");

        if (!string.IsNullOrWhiteSpace(request.TenantId)
            && !string.Equals(request.TenantId, tenant.Id, StringComparison.OrdinalIgnoreCase)
            && !string.Equals(request.TenantId, tenant.Identifier, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("tenant_id does not match the current tenant.");
        }

        var state = await workflowStore.GetAsync(tenant.Id!, request.WorkflowId, cancellationToken)
            ?? throw new KeyNotFoundException($"Workflow '{request.WorkflowId}' was not found or expired.");

        if (!string.Equals(state.Status, "awaiting_approval", StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException($"Workflow is not awaiting approval (status={state.Status}).");

        var workspaceId = await WorkspaceRequirement.RequireWorkspaceIdAsync(
            workspaceContext, db, cancellationToken);
        var (_, credentials) = await GravityTenantCredentials.LoadActiveAsync(
            db,
            logger,
            workspaceId,
            state.IntegrationId,
            cancellationToken);

        var created = await coordinator.CreateApprovedAsync(
            credentials,
            state.WorkflowId,
            state.Draft,
            request.Approvals,
            cancellationToken);

        var completed = state with { Status = "created" };
        await workflowStore.SaveAsync(completed, cancellationToken);

        logger.LogInformation(
            "Onboarding approved and created. TenantId={TenantId} WorkflowId={WorkflowId} ProductId={ProductId}",
            tenant.Id,
            created.WorkflowId,
            created.ProductId);

        return new ApproveOnboardingResult(
            created.WorkflowId,
            created.BrandId,
            created.CategoryId,
            created.ProductId);
    }
}
