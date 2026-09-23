using Commerce.Application.Abstracts;
using Commerce.Application.Common.Abstracts;

namespace Commerce.Application.Features.Integrations.GetAvailableIntegrations;

public sealed class GetAvailableIntegrationsHandler(IIntegrationsMetadataService metadataService)
    : IQueryHandler<GetAvailableIntegrationsQuery, IReadOnlyList<IntegrationMetadataDto>>
{
    public Task<IReadOnlyList<IntegrationMetadataDto>> Handle(
        GetAvailableIntegrationsQuery request,
        CancellationToken cancellationToken) =>
        metadataService.GetAvailableIntegrationsAsync(cancellationToken);
}
