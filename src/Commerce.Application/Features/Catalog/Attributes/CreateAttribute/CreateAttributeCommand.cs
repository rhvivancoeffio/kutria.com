using Commerce.Application.Common.Abstracts;

namespace Commerce.Application.Features.Catalog.Attributes.CreateAttribute;

public sealed record CreateAttributeCommand(
    string EntityName,
    string Name,
    bool IsMultiOption,
    int SpecificationType,
    bool Required,
    string? Description = null,
    string? Label = null,
    string? Values = null,
    int Order = 0,
    bool IsPublic = false,
    string? SectionGroup = null,
    Guid? IntegrationId = null) : ICommand<CreateAttributeResult>;
