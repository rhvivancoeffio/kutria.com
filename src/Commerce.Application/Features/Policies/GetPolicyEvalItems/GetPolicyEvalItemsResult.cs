namespace Commerce.Application.Features.Policies.GetPolicyEvalItems;

public sealed record GetPolicyEvalItemsResult(string Version, IReadOnlyList<PolicyEvalItemRow> Items);

public sealed record PolicyEvalItemRow(string? BaseItemId, Guid? ExtraId, string Type, string Question, bool IsDisabled, string Source);
