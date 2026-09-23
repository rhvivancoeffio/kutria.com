using Commerce.Application.Common.Abstracts;

namespace Commerce.Application.Features.DataIngestion.ListIngestionsCatalog;

public sealed record ListIngestionsCatalogQuery(
    Guid IntegrationId,
    int PageSize = 25,
    string? NextToken = null) : IQuery<ListIngestionsCatalogResult>;
