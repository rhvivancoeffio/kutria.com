namespace Commerce.Application.Abstracts;

public sealed record PolicyEvalQuestion(string Id, string Type, string Question, string Source);

public sealed record PolicyEvalSet(string Version, IReadOnlyList<PolicyEvalQuestion> Questions);

public sealed record PolicyGoldenItem(string Id, string Type, string Question);

public interface IPolicyEvalCatalog
{
    IReadOnlyList<PolicyGoldenItem> LoadBase();

    Task<PolicyEvalSet> ResolveAsync(
        string? type = null,
        Guid? workspaceId = null,
        CancellationToken cancellationToken = default);
}

public sealed record PolicyEvalVerdict(bool Invented, string Reason);

public interface IPolicyEvalModel
{
    bool IsAvailable { get; }

    Task<string> AnswerAsync(string question, string toolResultJson, CancellationToken cancellationToken = default);

    Task<PolicyEvalVerdict> JudgeAsync(
        string question,
        string toolResultJson,
        string answer,
        CancellationToken cancellationToken = default);
}

public interface IPolicyEvalSearch
{
    Task<string> SearchCandidateAsync(
        string tenantId,
        Guid? workspaceId,
        Guid jobId,
        string query,
        string? type,
        CancellationToken cancellationToken = default);
}
