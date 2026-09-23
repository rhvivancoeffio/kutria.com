using Commerce.Application.Common.Abstracts;

namespace Commerce.Application.Features.ApiKeys.CreateApiKey;

public sealed record CreateApiKeyCommand(string Name) : ICommand<CreateApiKeyResult>;
