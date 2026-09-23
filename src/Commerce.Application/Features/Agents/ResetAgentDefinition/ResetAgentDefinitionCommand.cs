using Commerce.Application.Common.Abstracts;

namespace Commerce.Application.Features.Agents.ResetAgentDefinition;

public sealed record ResetAgentDefinitionCommand(string AgentKey) : ICommand<ResetAgentDefinitionResult>;
