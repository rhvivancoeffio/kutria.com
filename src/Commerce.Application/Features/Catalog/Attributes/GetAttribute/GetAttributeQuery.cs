using Commerce.Application.Common.Abstracts;

namespace Commerce.Application.Features.Catalog.Attributes.GetAttribute;

public sealed record GetAttributeQuery(string EntityAttributeId, Guid? IntegrationId = null) : IQuery<GetAttributeResult?>;
