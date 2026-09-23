using Commerce.Application.Common.Abstracts;

namespace Commerce.Application.Features.DataIngestion.EnqueueDataIngest;

public sealed record EnqueueDataIngestCommand(Guid IntegrationId, string? Kind = null)
    : ICommand<EnqueueDataIngestResult>;
