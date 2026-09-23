using Commerce.Application.Common.Abstracts;

namespace Commerce.Application.Features.Policies.RunPolicyEval;

public sealed record RunPolicyEvalCommand(Guid JobId) : ICommand<RunPolicyEvalResult>;
