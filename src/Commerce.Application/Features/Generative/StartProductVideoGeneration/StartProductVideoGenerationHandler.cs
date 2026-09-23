using System.Text.Json;
using Finbuckle.MultiTenant.Abstractions;
using Commerce.Application.Abstracts;
using Commerce.Application.Common.Abstracts;
using Commerce.Domain.Tenants;

namespace Commerce.Application.Features.Generative.StartProductVideoGeneration;

public sealed class StartProductVideoGenerationHandler(
    IEventStreamStore eventStreams,
    IMultiTenantContextAccessor<CommerceTenantInfo> tenantAccessor)
    : ICommandHandler<StartProductVideoGenerationCommand, StartProductVideoGenerationResult>
{
    public const string AgentKey = "product-video-generator";
    public const string Kind = "generative.video";

    public async Task<StartProductVideoGenerationResult> Handle(
        StartProductVideoGenerationCommand request,
        CancellationToken cancellationToken)
    {
        var tenant = tenantAccessor.MultiTenantContext?.TenantInfo
            ?? throw new InvalidOperationException("Tenant is required.");
        var tenantId = tenant.Id ?? tenant.Identifier
            ?? throw new InvalidOperationException("Tenant id is required.");

        var summary = JsonSerializer.Serialize(new
        {
            kind = Kind,
            name = request.Name.Trim(),
            hasImage = !string.IsNullOrWhiteSpace(request.ImageUrl)
        });

        var processId = await eventStreams.StartProcessAsync(tenantId, Kind, summary, cancellationToken);
        var prompt =
            $"Genera guion UGC stub para: {request.Name.Trim()}. "
            + $"Descripción: {(request.Description ?? "").Trim()}. "
            + "Llama generate_product_video una vez y responde con el JSON del schema.";

        return new StartProductVideoGenerationResult(processId, AgentKey, prompt, Kind);
    }
}
