using Commerce.Application.Common.Abstracts;

namespace Commerce.Application.Features.DataIngestion.GetIngestedCatalogItem;

public sealed record GetIngestedCatalogItemQuery(Guid IntegrationId, string Id)
    : IQuery<GetIngestedCatalogItemResult?>;
