namespace Commerce.Application.Features.Agents.SaveAgentDefinition;

public sealed record SaveAgentDefinitionResult(string Key, string Hash, bool IsTenantOverride);
