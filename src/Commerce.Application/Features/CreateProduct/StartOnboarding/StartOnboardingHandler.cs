using Finbuckle.MultiTenant.Abstractions;
using Microsoft.Extensions.Logging;
using Commerce.Application.Abstracts;
using Commerce.Application.Common;
using Commerce.Application.Common.Abstracts;
using Commerce.Application.Features.CreateProduct;
using Commerce.Domain.Tenants;

namespace Commerce.Application.Features.CreateProduct.StartOnboarding;

public sealed class StartOnboardingHandler(
    ICommerceDbContext db,
    IWorkspaceContext workspaceContext,
    IProductOnboardingCoordinator coordinator,
    IOnboardingWorkflowStore workflowStore,
    IOnboardingImageStore imageStore,
    IMultiTenantContextAccessor<CommerceTenantInfo> tenantAccessor,
    ILogger<StartOnboardingHandler> logger)
    : ICommandHandler<StartOnboardingCommand, StartOnboardingResult>
{
    public async Task<StartOnboardingResult> Handle(StartOnboardingCommand request, CancellationToken cancellationToken)
    {
        var tenant = tenantAccessor.MultiTenantContext?.TenantInfo
            ?? throw new InvalidOperationException("Tenant is required.");

        var workspaceId = await WorkspaceRequirement.RequireWorkspaceIdAsync(
            workspaceContext, db, cancellationToken);
        var (integration, credentials) = await GravityTenantCredentials.LoadActiveAsync(
            db,
            logger,
            workspaceId,
            request.IntegrationId,
            cancellationToken);

        var workflowId = $"wf_{Guid.NewGuid():N}";
        string? imageUrl = string.IsNullOrWhiteSpace(request.ImageUrl) ? null : request.ImageUrl.Trim();
        Stream? analyzeStream = request.ImageStream;

        if (request.ImageStream is not null)
        {
            await using var copy = new MemoryStream();
            if (request.ImageStream.CanSeek)
                request.ImageStream.Position = 0;
            await request.ImageStream.CopyToAsync(copy, cancellationToken);
            var bytes = copy.ToArray();
            if (bytes.Length > 0)
            {
                await imageStore.SaveAsync(
                    tenant.Id!,
                    workflowId,
                    bytes,
                    request.ImageContentType ?? "image/jpeg",
                    cancellationToken);
                analyzeStream = new MemoryStream(bytes);
                var relative = $"/t/{tenant.Identifier}/products/onboarding/{workflowId}/image";
                imageUrl ??= string.IsNullOrWhiteSpace(request.PublicBaseUrl)
                    ? relative
                    : $"{request.PublicBaseUrl.TrimEnd('/')}{relative}";
            }
        }

        var draft = await coordinator.AnalyzeAsync(
            credentials,
            request.Title,
            imageUrl,
            analyzeStream,
            request.ImageContentType,
            cancellationToken);

        if (analyzeStream is MemoryStream owned)
            await owned.DisposeAsync();

        var state = new OnboardingWorkflowState(
            workflowId,
            tenant.Id!,
            integration.Id,
            draft,
            "awaiting_approval",
            DateTimeOffset.UtcNow);

        await workflowStore.SaveAsync(state, cancellationToken);

        logger.LogInformation(
            "Onboarding started. TenantId={TenantId} WorkflowId={WorkflowId} IntegrationId={IntegrationId}",
            tenant.Id,
            workflowId,
            integration.Id);

        return new StartOnboardingResult(workflowId, tenant.Id!, draft);
    }
}
