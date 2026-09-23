using Commerce.Application.Common.Abstracts;

namespace Commerce.Application.Features.Policies.SavePolicyEvalItems;

public sealed record SavePolicyEvalItemsCommand(IReadOnlyList<SavePolicyEvalItem> Items) : ICommand<SavePolicyEvalItemsResult>;

public sealed record SavePolicyEvalItem(string? BaseItemId, Guid? ExtraId, string? Type, string Question, bool IsDisabled);
