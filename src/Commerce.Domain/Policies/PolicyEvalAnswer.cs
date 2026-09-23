using Commerce.Domain.Common;

namespace Commerce.Domain.Policies;

public class PolicyEvalAnswer : BaseEntity
{
    public Guid RunId { get; set; }
    public string QuestionId { get; set; } = string.Empty;
    public string Question { get; set; } = string.Empty;
    public string ToolResultJson { get; set; } = "{}";
    public string Answer { get; set; } = string.Empty;
    public bool Invented { get; set; }
    public string? JudgeReason { get; set; }
    public PolicyEvalRun? Run { get; set; }
}
