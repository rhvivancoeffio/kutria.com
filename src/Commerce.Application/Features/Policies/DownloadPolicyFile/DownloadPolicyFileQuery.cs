using Commerce.Application.Common.Abstracts;

namespace Commerce.Application.Features.Policies.DownloadPolicyFile;

public sealed record DownloadPolicyFileQuery(Guid JobId) : IQuery<DownloadPolicyFileResult?>;
