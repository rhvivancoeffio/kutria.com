using Commerce.Application.Abstracts;
using Commerce.Infrastructure.Agents.Definitions.PostSalesSupport.Tools;

namespace Commerce.Infrastructure.Policies;

public sealed class PolicyEvalSearch(IBrainIndex index, IBrainProfileRegistry profiles) : IPolicyEvalSearch
{
    public Task<string> SearchCandidateAsync(
        string tenantId,
        Guid? workspaceId,
        Guid jobId,
        string query,
        string? type,
        CancellationToken cancellationToken = default)
        => SearchPoliciesTool.SearchCandidateAsync(
            index, profiles, tenantId, workspaceId, jobId, query, type, cancellationToken);
}
