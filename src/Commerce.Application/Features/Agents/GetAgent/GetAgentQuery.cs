using Commerce.Application.Common.Abstracts;

namespace Commerce.Application.Features.Agents.GetAgent;

public sealed record GetAgentQuery(string AgentKey) : IQuery<GetAgentResult?>;
