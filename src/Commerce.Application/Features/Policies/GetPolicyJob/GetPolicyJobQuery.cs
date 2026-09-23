using Commerce.Application.Common.Abstracts;

namespace Commerce.Application.Features.Policies.GetPolicyJob;

public sealed record GetPolicyJobQuery(Guid JobId) : IQuery<GetPolicyJobResult?>;
