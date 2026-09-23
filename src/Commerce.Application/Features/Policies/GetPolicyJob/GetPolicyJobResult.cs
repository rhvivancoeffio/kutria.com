namespace Commerce.Application.Features.Policies.GetPolicyJob;

public sealed record GetPolicyJobResult(
    Guid Id,
    string Status,
    string FileName,
    int? PageCount,
    int? TableCount,
    string? Text,
    string? Summary,
    string? Error);
