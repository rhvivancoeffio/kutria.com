namespace Commerce.Application.Features.DataIngestion.EnqueueDataIngest;

public sealed record EnqueueDataIngestResult(Guid IntegrationId, IReadOnlyList<string> EnqueuedKinds);
