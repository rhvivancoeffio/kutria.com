using Commerce.Application.Common.Abstracts;

namespace Commerce.Application.Features.DataIngestion.ListIngestedOrders;

public sealed record ListIngestedOrdersQuery(Guid IntegrationId, int Page = 1, int PageSize = 25)
    : IQuery<ListIngestedOrdersResult>;
