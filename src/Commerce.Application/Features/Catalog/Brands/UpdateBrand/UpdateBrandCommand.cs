using Commerce.Application.Common.Abstracts;

namespace Commerce.Application.Features.Catalog.Brands.UpdateBrand;

public sealed record UpdateBrandCommand(
    string BrandId,
    string Name,
    string? Description = null,
    bool IsActive = true,
    string? ImageUrl = null,
    Guid? IntegrationId = null) : ICommand<UpdateBrandResult>;
