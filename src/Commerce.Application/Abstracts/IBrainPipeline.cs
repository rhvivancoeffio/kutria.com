namespace Commerce.Application.Abstracts;

public static class BrainKeys
{
    public const string Policy = "policy";
}

public static class BrainQueues
{
    public const string Ingest = "brain-ingest";
    public const string PolicyEval = "policy-eval";
}

public sealed record PolicyEvalMessage(Guid JobId, string Tenant);

public sealed record BrainIngestMessage(Guid JobId, string Tenant, string Step)
{
    public const string Parse = "parse";
    public const string Publish = "publish";
}

public sealed record BrainUploadRequest(
    string BrainKey,
    string FileName,
    string ContentType,
    byte[] Content,
    string MetadataJson);

public sealed record BrainJobResult(
    Guid Id,
    string BrainKey,
    string Status,
    string FileName,
    int? PageCount,
    int? TableCount,
    string? Text,
    string? Summary,
    string? Error);

public interface IBrainPipeline
{
    Task<BrainJobResult> UploadAsync(BrainUploadRequest request, CancellationToken cancellationToken = default);

    Task<BrainJobResult?> GetAsync(Guid jobId, CancellationToken cancellationToken = default);

    Task<BrainJobResult> PublishAsync(Guid jobId, string? editedText, CancellationToken cancellationToken = default);
}
