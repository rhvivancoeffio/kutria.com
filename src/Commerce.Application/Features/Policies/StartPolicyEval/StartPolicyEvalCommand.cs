using Commerce.Application.Common.Abstracts;

namespace Commerce.Application.Features.Policies.StartPolicyEval;

public sealed record StartPolicyEvalCommand(Guid JobId) : ICommand<StartPolicyEvalResult>;
