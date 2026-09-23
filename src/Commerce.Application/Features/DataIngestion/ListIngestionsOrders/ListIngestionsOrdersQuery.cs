using Commerce.Application.Common.Abstracts;

namespace Commerce.Application.Features.DataIngestion.ListIngestionsOrders;

public sealed record ListIngestionsOrdersQuery(
    Guid IntegrationId,
    int PageSize = 25,
    string? NextToken = null) : IQuery<ListIngestionsOrdersResult>;
