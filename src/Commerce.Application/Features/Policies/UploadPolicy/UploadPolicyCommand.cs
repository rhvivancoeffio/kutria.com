using Commerce.Application.Common.Abstracts;

namespace Commerce.Application.Features.Policies.UploadPolicy;

public sealed record UploadPolicyCommand(
    string FileName,
    string ContentType,
    byte[] Content,
    string Type,
    string EffectiveFrom) : ICommand<UploadPolicyResult>;
