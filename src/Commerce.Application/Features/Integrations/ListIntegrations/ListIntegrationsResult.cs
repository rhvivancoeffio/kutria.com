namespace Commerce.Application.Features.Integrations.ListIntegrations;

public sealed record ListIntegrationsResult(IReadOnlyList<IntegrationListItem> Items);
