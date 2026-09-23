using Commerce.Application.Common.Abstracts;

namespace Commerce.Application.Features.Policies.PublishPolicy;

public sealed record PublishPolicyCommand(Guid JobId, string? EditedText) : ICommand<PublishPolicyResult>;
