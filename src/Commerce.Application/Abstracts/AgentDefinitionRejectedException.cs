namespace Commerce.Application.Abstracts;

public sealed class AgentDefinitionRejectedException : Exception
{
    public AgentDefinitionRejectedException(string message) : base(message)
    {
    }
}
