namespace Commerce.Domain.Brains;

public static class PolicyEvaluationStatus
{
    public const string NotRun = "not_run";
    public const string Pending = "pending";
    public const string Passed = "passed";
    public const string ReviewFailed = "review_failed";
    public const int MaxFailed = 2;
}
