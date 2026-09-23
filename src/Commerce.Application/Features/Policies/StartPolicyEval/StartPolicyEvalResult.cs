namespace Commerce.Application.Features.Policies.StartPolicyEval;

public sealed record StartPolicyEvalResult(Guid JobId, string Status, bool Started, string StreamId);
