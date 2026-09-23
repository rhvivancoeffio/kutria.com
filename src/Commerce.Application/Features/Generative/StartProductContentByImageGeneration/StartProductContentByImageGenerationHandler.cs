using System.Text.Json;
using Finbuckle.MultiTenant.Abstractions;
using Commerce.Application.Abstracts;
using Commerce.Application.Common.Abstracts;
using Commerce.Domain.Tenants;

namespace Commerce.Application.Features.Generative.StartProductContentByImageGeneration;

public sealed class StartProductContentByImageGenerationHandler(
    IEventStreamStore eventStreams,
    IMultiTenantContextAccessor<CommerceTenantInfo> tenantAccessor)
    : ICommandHandler<StartProductContentByImageGenerationCommand, StartProductContentByImageGenerationResult>
{
    public const string AgentKey = "product-content-by-image-generator";
    public const string Kind = "generative.content-by-image";

    public async Task<StartProductContentByImageGenerationResult> Handle(
        StartProductContentByImageGenerationCommand request,
        CancellationToken cancellationToken)
    {
        var tenant = tenantAccessor.MultiTenantContext?.TenantInfo
            ?? throw new InvalidOperationException("Tenant is required.");
        var tenantId = tenant.Id ?? tenant.Identifier
            ?? throw new InvalidOperationException("Tenant id is required.");

        var brandId = NullIfWhiteSpace(request.BrandId);
        var brandName = NullIfWhiteSpace(request.BrandName);
        var categoryId = NullIfWhiteSpace(request.CategoryId);
        var categoryPath = NullIfWhiteSpace(request.CategoryPath);

        var summary = JsonSerializer.Serialize(new
        {
            kind = Kind,
            hasImage = true,
            imageUrl = request.ImageUrl,
            imageAttachmentId = request.ImageAttachmentId,
            hint = request.Hint,
            brandId,
            brandName,
            categoryId,
            categoryPath
        });

        var processId = await eventStreams.StartProcessAsync(tenantId, Kind, summary, cancellationToken);
        var hint = string.IsNullOrWhiteSpace(request.Hint) ? "" : $" Hint: {request.Hint.Trim()}.";
        var catalogCtx = GenerativeCatalogContextPrompt.Build(brandId, brandName, categoryId, categoryPath);
        var prompt =
            "Genera ficha completa desde la imagen del turno."
            + hint
            + catalogCtx
            + " Llama generate_product_content_from_image una vez y responde con el JSON del schema.";

        return new StartProductContentByImageGenerationResult(
            processId,
            AgentKey,
            prompt,
            Kind,
            request.ImageUrl,
            request.ImageAttachmentId);
    }

    private static string? NullIfWhiteSpace(string? value)
        => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
