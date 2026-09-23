namespace Commerce.Application.Features.DataIngestion.ProcessDataIngest;

public sealed record ProcessDataIngestResult(Guid IntegrationId, string Kind, int Upserted);
