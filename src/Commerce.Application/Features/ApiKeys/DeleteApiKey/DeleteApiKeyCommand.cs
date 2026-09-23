using Commerce.Application.Common.Abstracts;

namespace Commerce.Application.Features.ApiKeys.DeleteApiKey;

public sealed record DeleteApiKeyCommand(Guid Id) : ICommand<DeleteApiKeyResult>;
