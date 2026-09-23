namespace Commerce.Application.Features.Policies.RunPolicyEval;

public sealed record RunPolicyEvalResult(Guid JobId, string Status, int FailedCount);
