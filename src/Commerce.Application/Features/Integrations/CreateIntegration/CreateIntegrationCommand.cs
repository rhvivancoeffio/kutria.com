using System.Text.Json;
using Commerce.Application.Common.Abstracts;

namespace Commerce.Application.Features.Integrations.CreateIntegration;

public sealed record CreateIntegrationCommand(
    string Provider,
    string Name,
    Dictionary<string, JsonElement>? Settings = null) : ICommand<CreateIntegrationResult>;
