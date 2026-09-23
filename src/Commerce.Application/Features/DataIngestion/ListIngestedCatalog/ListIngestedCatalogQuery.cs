using Commerce.Application.Common.Abstracts;

namespace Commerce.Application.Features.DataIngestion.ListIngestedCatalog;

public sealed record ListIngestedCatalogQuery(Guid IntegrationId, int Page = 1, int PageSize = 25)
    : IQuery<ListIngestedCatalogResult>;
