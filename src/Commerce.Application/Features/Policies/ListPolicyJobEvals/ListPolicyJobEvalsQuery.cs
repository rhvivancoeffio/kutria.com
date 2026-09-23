using Commerce.Application.Common.Abstracts;

namespace Commerce.Application.Features.Policies.ListPolicyJobEvals;

public sealed record ListPolicyJobEvalsQuery(Guid JobId) : IQuery<ListPolicyJobEvalsResult>;
