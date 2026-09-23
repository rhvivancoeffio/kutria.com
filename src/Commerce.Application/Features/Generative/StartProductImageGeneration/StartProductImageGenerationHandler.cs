using System.Text.Json;
using Finbuckle.MultiTenant.Abstractions;
using Commerce.Application.Abstracts;
using Commerce.Application.Common.Abstracts;
using Commerce.Domain.Tenants;

namespace Commerce.Application.Features.Generative.StartProductImageGeneration;

public sealed class StartProductImageGenerationHandler(
    IEventStreamStore eventStreams,
    IMultiTenantContextAccessor<CommerceTenantInfo> tenantAccessor)
    : ICommandHandler<StartProductImageGenerationCommand, StartProductImageGenerationResult>
{
    public const string AgentKey = "product-image-generator";
    public const string Kind = "generative.images";

    public async Task<StartProductImageGenerationResult> Handle(
        StartProductImageGenerationCommand request,
        CancellationToken cancellationToken)
    {
        var tenant = tenantAccessor.MultiTenantContext?.TenantInfo
            ?? throw new InvalidOperationException("Tenant is required.");
        var tenantId = tenant.Id ?? tenant.Identifier
            ?? throw new InvalidOperationException("Tenant id is required.");

        var count = Math.Clamp(request.Count, 1, 2);
        var summary = JsonSerializer.Serialize(new
        {
            kind = Kind,
            name = request.Name.Trim(),
            count
        });

        var processId = await eventStreams.StartProcessAsync(tenantId, Kind, summary, cancellationToken);
        var prompt =
            $"Genera {count} imagen(es) de catálogo para: {request.Name.Trim()}. "
            + $"Descripción: {(request.Description ?? "").Trim()}. "
            + "Llama generate_product_images una vez y responde con el JSON del schema.";

        return new StartProductImageGenerationResult(processId, AgentKey, prompt, Kind);
    }
}
