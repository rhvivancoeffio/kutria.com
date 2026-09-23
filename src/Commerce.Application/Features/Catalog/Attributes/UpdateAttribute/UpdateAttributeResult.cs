namespace Commerce.Application.Features.Catalog.Attributes.UpdateAttribute;

public sealed record UpdateAttributeResult(
    string EntityAttributeId,
    string? EntityName,
    string? Name,
    string? Label,
    int SpecificationType,
    string? SpecificationTypeName);
