using Commerce.Application.Common.Abstracts;

namespace Commerce.Application.Features.Integrations.DeleteIntegration;

public sealed record DeleteIntegrationCommand(Guid Id) : ICommand<DeleteIntegrationResult>;
