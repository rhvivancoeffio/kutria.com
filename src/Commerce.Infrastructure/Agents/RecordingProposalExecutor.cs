using Commerce.Application.Abstracts;
using Commerce.Domain.Agents;

namespace Commerce.Infrastructure.Agents;

public sealed class RecordingProposalExecutor : IProposalExecutor
{
    public Task ExecuteAsync(Proposal proposal, CancellationToken cancellationToken = default)
    {
        proposal.Status = Proposal.Executed;
        proposal.ExecutedAt = DateTimeOffset.UtcNow;
        return Task.CompletedTask;
    }
}
