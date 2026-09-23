using Commerce.Application.Common.Abstracts;

namespace Commerce.Application.Features.Agents.SaveAgentDefinition;

public sealed record SaveAgentDefinitionCommand(string AgentKey, string Yaml) : ICommand<SaveAgentDefinitionResult>;
