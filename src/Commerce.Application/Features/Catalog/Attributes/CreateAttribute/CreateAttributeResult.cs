namespace Commerce.Application.Features.Catalog.Attributes.CreateAttribute;

public sealed record CreateAttributeResult(
    string EntityAttributeId,
    string? EntityName,
    string? Name,
    string? Label,
    int SpecificationType,
    string? SpecificationTypeName);
