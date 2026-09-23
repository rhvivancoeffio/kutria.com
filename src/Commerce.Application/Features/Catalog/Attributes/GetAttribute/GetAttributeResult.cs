namespace Commerce.Application.Features.Catalog.Attributes.GetAttribute;

public sealed record GetAttributeResult(
    string EntityAttributeId,
    string? EntityName,
    string? Key,
    string? Name,
    string? Description,
    string? Label,
    string? Values,
    bool IsMultiOption,
    int SpecificationType,
    string? SpecificationTypeName,
    int Order,
    bool Required,
    bool IsPublic,
    string? SectionGroup);
