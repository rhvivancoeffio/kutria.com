using Commerce.Application.Common.Abstracts;

namespace Commerce.Application.Features.Catalog.Brands.CreateBrand;

public sealed record CreateBrandCommand(
    string Name,
    string? Description = null,
    bool IsActive = true,
    string? ImageUrl = null,
    Guid? IntegrationId = null) : ICommand<CreateBrandResult>;
