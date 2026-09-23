using System.Text.Json;
using Commerce.Application.Common.Abstracts;

namespace Commerce.Application.Features.Integrations.UpdateIntegration;

public sealed record UpdateIntegrationCommand(
    Guid Id,
    string? Name = null,
    bool? IsActive = null,
    Dictionary<string, JsonElement>? Settings = null) : ICommand<UpdateIntegrationResult>;
