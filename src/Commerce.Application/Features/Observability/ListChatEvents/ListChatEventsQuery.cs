using Commerce.Application.Common.Abstracts;

namespace Commerce.Application.Features.Observability.ListChatEvents;

public sealed record ListChatEventsQuery(
    int PageSize = 25,
    string? NextToken = null,
    string? ThreadId = null,
    string? EventType = null,
    bool? EmptyOnly = null) : IQuery<ListChatEventsResult>;
