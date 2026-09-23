namespace Commerce.Application.Features.Policies.ListPublishedPolicies;

public sealed record ListPublishedPoliciesResult(IReadOnlyList<PublishedPolicyDocument> Items);

public sealed record PublishedPolicyDocument(
    Guid Id,
    string Type,
    string FileName,
    string? EffectiveFrom,
    DateTimeOffset UploadedAt,
    int? PageCount,
    int? TableCount,
    string? Summary,
    long? SizeBytes,
    string EvaluationStatus,
    int EvaluationFailedCount);
