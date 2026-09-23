using Commerce.Application.Common.Abstracts;

namespace Commerce.Application.Features.ApiKeys.RevokeApiKey;

public sealed record RevokeApiKeyCommand(Guid Id) : ICommand<RevokeApiKeyResult>;
