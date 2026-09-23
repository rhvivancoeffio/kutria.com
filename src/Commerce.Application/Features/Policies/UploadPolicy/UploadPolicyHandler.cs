using System.Text.Json;
using Commerce.Application.Abstracts;
using Commerce.Application.Common.Abstracts;

namespace Commerce.Application.Features.Policies.UploadPolicy;

public sealed class UploadPolicyHandler(IBrainPipeline pipeline)
    : ICommandHandler<UploadPolicyCommand, UploadPolicyResult>
{
    public async Task<UploadPolicyResult> Handle(UploadPolicyCommand request, CancellationToken cancellationToken)
    {
        var effective = DateOnly.Parse(request.EffectiveFrom);
        var type = request.Type.Trim().ToLowerInvariant();
        var metadata = JsonSerializer.Serialize(new Dictionary<string, string>
        {
            ["type"] = type,
            ["effective_from"] = effective.ToString("yyyy-MM-dd"),
            ["effective_to"] = string.Empty,
            ["language"] = "es-PE",
            ["applicable_sellers"] = "all",
            ["title"] = Path.GetFileNameWithoutExtension(request.FileName),
            ["policy_id"] = $"{type}_{effective:yyyyMMdd}"
        });
        var job = await pipeline.UploadAsync(
            new BrainUploadRequest(BrainKeys.Policy, request.FileName, request.ContentType, request.Content, metadata),
            cancellationToken);
        return new UploadPolicyResult(job.Id, job.Status);
    }
}
