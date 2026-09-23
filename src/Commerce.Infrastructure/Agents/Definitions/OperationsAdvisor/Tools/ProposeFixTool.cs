using Microsoft.Extensions.AI;
using Microsoft.Extensions.DependencyInjection;
using Commerce.Application.Abstracts;
using Commerce.Domain.Agents;

namespace Commerce.Infrastructure.Agents.Definitions.OperationsAdvisor.Tools;

internal static class ProposeFixTool
{
    public const string Name = "propose_fix";

    public static AITool Create(IServiceProvider services, string tenantId, string agentKey)
    {
        var db = services.GetRequiredService<ICommerceDbContext>();
        return AIFunctionFactory.Create(
            async (string title, string action, string impact, CancellationToken cancellationToken) =>
            {
                var proposal = new Proposal
                {
                    TenantId = tenantId,
                    AgentKey = agentKey,
                    Title = title.Trim(),
                    Action = action.Trim(),
                    Impact = impact.Trim(),
                    Status = Proposal.Pending
                };
                db.Proposals.Add(proposal);
                await db.SaveChangesAsync(cancellationToken);
                return proposal.Id.ToString();
            },
            name: Name,
            description: AgentTools.Description(services, Name));
    }
}
