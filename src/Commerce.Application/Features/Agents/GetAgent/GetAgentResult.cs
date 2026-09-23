namespace Commerce.Application.Features.Agents.GetAgent;

public sealed record GetAgentResult(string Key, string Yaml, bool IsTenantOverride, string Hash);
