using Commerce.Application.Common.Abstracts;

namespace Commerce.Application.Features.Policies.GetPolicyEvalStatus;

public sealed record GetPolicyEvalStatusQuery(Guid JobId) : IQuery<GetPolicyEvalStatusResult?>;
