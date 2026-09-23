using System.Text.Json;
using Finbuckle.MultiTenant.Abstractions;
using Commerce.Application.Abstracts;
using Commerce.Application.Common.Abstracts;
using Commerce.Domain.Tenants;

namespace Commerce.Application.Features.Generative.StartProductContentGeneration;

public sealed class StartProductContentGenerationHandler(
    IEventStreamStore eventStreams,
    IMultiTenantContextAccessor<CommerceTenantInfo> tenantAccessor)
    : ICommandHandler<StartProductContentGenerationCommand, StartProductContentGenerationResult>
{
    public const string AgentKey = "product-content-generator";
    public const string Kind = "generative.content";

    public async Task<StartProductContentGenerationResult> Handle(
        StartProductContentGenerationCommand request,
        CancellationToken cancellationToken)
    {
        var tenant = tenantAccessor.MultiTenantContext?.TenantInfo
            ?? throw new InvalidOperationException("Tenant is required.");
        var tenantId = tenant.Id ?? tenant.Identifier
            ?? throw new InvalidOperationException("Tenant id is required.");

        var modes = request.Modes is { Count: > 0 }
            ? request.Modes.Select(m => m.Trim()).Where(m => m.Length > 0).ToArray()
            : ["all"];
        var brandId = NullIfWhiteSpace(request.BrandId);
        var brandName = NullIfWhiteSpace(request.BrandName);
        var categoryId = NullIfWhiteSpace(request.CategoryId);
        var categoryPath = NullIfWhiteSpace(request.CategoryPath);

        var summary = JsonSerializer.Serialize(new
        {
            kind = Kind,
            name = request.Name.Trim(),
            modes,
            hasImage = false,
            brandId,
            brandName,
            categoryId,
            categoryPath
        });

        var processId = await eventStreams.StartProcessAsync(tenantId, Kind, summary, cancellationToken);
        var modesText = string.Join(", ", modes);
        var catalogCtx = GenerativeCatalogContextPrompt.Build(brandId, brandName, categoryId, categoryPath);
        var prompt =
            $"Genera ficha de producto. Nombre: {request.Name.Trim()}. Modos: {modesText}."
            + catalogCtx
            + " Llama generate_product_content una vez y responde con el JSON del schema.";

        return new StartProductContentGenerationResult(processId, AgentKey, prompt, Kind);
    }

    private static string? NullIfWhiteSpace(string? value)
        => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
