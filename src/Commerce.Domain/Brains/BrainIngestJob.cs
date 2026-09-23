using Commerce.Domain.Common;

namespace Commerce.Domain.Brains;

public class BrainIngestJob : BaseEntity, IWorkspaceScoped
{
    public const string Queued = "queued";
    public const string Reading = "reading";
    public const string Ready = "ready";
    public const string NeedsOcr = "needs_ocr";
    public const string Publishing = "publishing";
    public const string Published = "published";
    public const string Failed = "failed";

    public string TenantId { get; set; } = string.Empty;
    public Guid? WorkspaceId { get; set; }
    public string BrainKey { get; set; } = string.Empty;
    public string FileName { get; set; } = string.Empty;
    public string StorageKey { get; set; } = string.Empty;
    public string Status { get; set; } = Queued;
    public string? ExtractedText { get; set; }
    public string? ConfirmedText { get; set; }
    public int? PageCount { get; set; }
    public int? TableCount { get; set; }
    public string? Summary { get; set; }
    public string? Error { get; set; }
    public string MetadataJson { get; set; } = "{}";
    public string EvaluationStatus { get; set; } = PolicyEvaluationStatus.NotRun;
    public int EvaluationFailedCount { get; set; }
}
