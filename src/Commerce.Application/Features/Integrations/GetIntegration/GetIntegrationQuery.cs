using Commerce.Application.Common.Abstracts;

namespace Commerce.Application.Features.Integrations.GetIntegration;

public sealed record GetIntegrationQuery(
    Guid Id,
    bool RevealSensitiveSettings = false) : IQuery<GetIntegrationResult?>;
