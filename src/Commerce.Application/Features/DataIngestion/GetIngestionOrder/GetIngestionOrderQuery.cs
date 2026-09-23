using Commerce.Application.Common.Abstracts;

namespace Commerce.Application.Features.DataIngestion.GetIngestionOrder;

public sealed record GetIngestionOrderQuery(Guid IntegrationId, string Id)
    : IQuery<GetIngestionOrderResult?>;
