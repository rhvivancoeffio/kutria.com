using Commerce.Application.Abstracts;
using Commerce.Application.Common.Abstracts;

namespace Commerce.Application.Features.Integrations.GetAvailableIntegrations;

public sealed record GetAvailableIntegrationsQuery : IQuery<IReadOnlyList<IntegrationMetadataDto>>;
