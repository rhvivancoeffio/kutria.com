using Commerce.Domain.Agents;

namespace Commerce.Application.Abstracts;

public interface IProposalExecutor
{
    Task ExecuteAsync(Proposal proposal, CancellationToken cancellationToken = default);
}
