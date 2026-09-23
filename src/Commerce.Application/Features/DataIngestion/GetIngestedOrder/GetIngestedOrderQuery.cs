using Commerce.Application.Common.Abstracts;

namespace Commerce.Application.Features.DataIngestion.GetIngestedOrder;

public sealed record GetIngestedOrderQuery(Guid IntegrationId, string Id)
    : IQuery<GetIngestedOrderResult?>;
