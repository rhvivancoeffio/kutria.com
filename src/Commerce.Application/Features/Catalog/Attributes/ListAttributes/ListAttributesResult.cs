namespace Commerce.Application.Features.Catalog.Attributes.ListAttributes;

public sealed record ListAttributesResult(IReadOnlyList<AttributeListItem> Items);

public sealed record AttributeListItem(
    string EntityAttributeId,
    string? EntityName,
    string? Key,
    string? Name,
    string? Label,
    int SpecificationType,
    string? SpecificationTypeName,
    bool IsMultiOption,
    bool Required,
    bool IsPublic,
    int Order,
    string? SectionGroup);
