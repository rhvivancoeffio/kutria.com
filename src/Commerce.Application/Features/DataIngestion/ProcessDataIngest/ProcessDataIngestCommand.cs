using Commerce.Application.Common.Abstracts;

namespace Commerce.Application.Features.DataIngestion.ProcessDataIngest;

public sealed record ProcessDataIngestCommand(Guid IntegrationId, string Kind)
    : ICommand<ProcessDataIngestResult>;
