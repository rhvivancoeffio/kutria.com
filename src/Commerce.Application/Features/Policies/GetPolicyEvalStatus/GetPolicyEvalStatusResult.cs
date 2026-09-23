namespace Commerce.Application.Features.Policies.GetPolicyEvalStatus;

public sealed record GetPolicyEvalStatusResult(Guid Id, string EvaluationStatus, int FailedCount, int Answered);
