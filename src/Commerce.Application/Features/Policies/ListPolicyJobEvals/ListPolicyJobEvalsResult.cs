namespace Commerce.Application.Features.Policies.ListPolicyJobEvals;

public sealed record ListPolicyJobEvalsResult(IReadOnlyList<PolicyEvalRunRow> Items);

public sealed record PolicyEvalRunRow(
    Guid Id,
    string TemplateVersion,
    string Status,
    int FailedCount,
    DateTimeOffset StartedAt,
    DateTimeOffset? FinishedAt,
    IReadOnlyList<PolicyEvalAnswerRow> Answers);

public sealed record PolicyEvalAnswerRow(string QuestionId, string Question, string ToolResultJson, string Answer, bool Invented, string? JudgeReason);
