using Microsoft.Extensions.AI;

namespace Commerce.Infrastructure.Agents;

internal sealed class RequiredPredecessorTool(
    AIFunction inner,
    IReadOnlyList<string> required,
    ToolCallTurn turn) : DelegatingAIFunction(inner)
{
    protected override async ValueTask<object?> InvokeCoreAsync(
        AIFunctionArguments arguments,
        CancellationToken cancellationToken)
    {
        var missing = required.Where(name => !turn.Called.Contains(name)).ToArray();
        if (missing.Length > 0)
        {
            throw new InvalidOperationException($"{Name} requires {string.Join(", ", missing)}.");
        }

        var result = await base.InvokeCoreAsync(arguments, cancellationToken);
        turn.Called.Add(Name);
        return result;
    }
}
