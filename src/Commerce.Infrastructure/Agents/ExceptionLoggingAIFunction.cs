using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;
using Commerce.Application.Abstracts;
using Commerce.Infrastructure.Agents.Definitions.DiscoveryAndCart.Tools;

namespace Commerce.Infrastructure.Agents;

/// <summary>Logs tool failures before rethrowing so MAF/LLM errors are visible in host logs.</summary>
internal sealed class ExceptionLoggingAIFunction(AIFunction inner, ILogger logger) : DelegatingAIFunction(inner)
{
    protected override async ValueTask<object?> InvokeCoreAsync(
        AIFunctionArguments arguments,
        CancellationToken cancellationToken)
    {
        try
        {
            return await base.InvokeCoreAsync(arguments, cancellationToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            logger.LogWarning(
                "Agent tool {ToolName} canceled (RequestAborted/SSE). Args={Args}",
                Name,
                FormatArgs(arguments));
            throw;
        }
        catch (CartException ex)
        {
            // Controlled cart/session miss — return JSON to the model (no fail:/retry storm).
            logger.LogWarning(
                "Agent tool {ToolName} cart error {Code}: {Message}. Args={Args}",
                Name,
                ex.Code,
                ex.Message,
                FormatArgs(arguments));
            return CartToolSupport.FormatError(ex);
        }
        catch (Exception ex)
        {
            logger.LogError(
                ex,
                "Agent tool {ToolName} failed. Args={Args}",
                Name,
                FormatArgs(arguments));
            throw;
        }
    }

    private static string FormatArgs(AIFunctionArguments arguments)
    {
        try
        {
            if (arguments.Count == 0)
                return "(none)";

            return string.Join(
                ", ",
                arguments.Select(kv =>
                {
                    var value = kv.Value switch
                    {
                        null => "null",
                        string s when s.Length > 80 => s[..80] + "…",
                        string s => s,
                        _ => kv.Value.ToString() ?? "?"
                    };
                    return $"{kv.Key}={value}";
                }));
        }
        catch
        {
            return "(unreadable)";
        }
    }
}
