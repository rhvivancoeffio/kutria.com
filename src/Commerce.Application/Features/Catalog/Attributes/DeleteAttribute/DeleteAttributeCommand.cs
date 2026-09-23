using Commerce.Application.Common.Abstracts;

namespace Commerce.Application.Features.Catalog.Attributes.DeleteAttribute;

public sealed record DeleteAttributeCommand(string EntityAttributeId, Guid? IntegrationId = null) : ICommand<DeleteAttributeResult>;
