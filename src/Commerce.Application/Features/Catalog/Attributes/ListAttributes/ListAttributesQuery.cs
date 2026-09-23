using Commerce.Application.Common.Abstracts;

namespace Commerce.Application.Features.Catalog.Attributes.ListAttributes;

public sealed record ListAttributesQuery(
    string? Name = null,
    int Page = 1,
    int PageSize = 20,
    Guid? IntegrationId = null) : IQuery<ListAttributesResult>;
