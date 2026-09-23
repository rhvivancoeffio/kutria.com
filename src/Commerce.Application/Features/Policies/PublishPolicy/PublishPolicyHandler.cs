using Commerce.Application.Abstracts;
using Commerce.Application.Common.Abstracts;

namespace Commerce.Application.Features.Policies.PublishPolicy;

public sealed class PublishPolicyHandler(IBrainPipeline pipeline)
    : ICommandHandler<PublishPolicyCommand, PublishPolicyResult>
{
    public async Task<PublishPolicyResult> Handle(PublishPolicyCommand request, CancellationToken cancellationToken)
    {
        var existing = await pipeline.GetAsync(request.JobId, cancellationToken)
            ?? throw new InvalidOperationException("Policy job was not found.");
        if (!string.Equals(existing.BrainKey, BrainKeys.Policy, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("Policy job was not found.");
        }

        var job = await pipeline.PublishAsync(request.JobId, request.EditedText, cancellationToken);
        return new PublishPolicyResult(job.Id, job.Status);
    }
}
