using Commerce.Application.Common.Abstracts;

namespace Commerce.Application.Features.DataIngestion.GetIngestionCatalogItem;

public sealed record GetIngestionCatalogItemQuery(Guid IntegrationId, string Id)
    : IQuery<GetIngestionCatalogItemResult?>;
