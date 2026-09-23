using Commerce.Application.Abstracts;
using Commerce.Application.Common.Abstracts;

namespace Commerce.Application.Features.Policies.GetPolicyJob;

public sealed class GetPolicyJobHandler(IBrainPipeline pipeline)
    : IQueryHandler<GetPolicyJobQuery, GetPolicyJobResult?>
{
    public async Task<GetPolicyJobResult?> Handle(GetPolicyJobQuery request, CancellationToken cancellationToken)
    {
        var job = await pipeline.GetAsync(request.JobId, cancellationToken);
        if (job is null || !string.Equals(job.BrainKey, BrainKeys.Policy, StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        return new GetPolicyJobResult(job.Id, job.Status, job.FileName, job.PageCount, job.TableCount, job.Text, job.Summary, job.Error);
    }
}
