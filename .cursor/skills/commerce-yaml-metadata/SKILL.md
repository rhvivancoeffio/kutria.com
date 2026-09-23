---
name: commerce-yaml-metadata
description: >-
  Host data YAML via IYamlMetadataService (generic I/O) and feature mappers like
  IntegrationsMetadataService. Use when adding seed/catalog YAML under data/,
  integration metadata, agent YAML loading, or YamlDotNet deserializers in Commerce.
---

# Commerce YAML metadata

## Roles

| Component | Responsibility |
|-----------|----------------|
| `IYamlMetadataService` / `YamlMetadataService` | Resolve `data/` root, read files, deserialize documents. **No** domain mapping. |
| Feature service (e.g. `IntegrationsMetadataService`) | Call YAML I/O, map private YAML models → application DTOs, cache if needed. |

Do **not** create a second `IDeserializer` inside feature services. Inject `IYamlMetadataService`.

## Naming profiles

- **CamelCase** (default): `DeserializeRequiredFromYamlDocument`, `DeserializeAsync`, `DeserializeRequiredAsync` — integrations (`data/integrations/*.integration.yaml`) and other host catalogs.
- **Agents only**: `DeserializeAgentYamlDocument` — underscored keys (`system_file`, `no_invent`) + converters in `Commerce.Infrastructure/Agents` (`AgentYamlDocument`, `AgentYamlConverters`).

## Integrations catalog

1. Add `data/integrations/{provider}.integration.yaml` (settings schema for the UI; no OpenAPI/Postman paths).
2. Ensure `Commerce.Api.csproj` copies `data/integrations/**/*.yaml` to output.
3. Mapping stays in `Commerce.Infrastructure/Integrations/IntegrationsMetadataService.cs`.
4. Expose via Application query + Carter (`GET .../integrations/available`).

## Anti-patterns

- Embedding vendor OpenAPI/Postman under `data/apis` for connect v1.
- Parsing integration YAML with the agent underscored deserializer (or the reverse).
- Domain DTO construction inside `YamlMetadataService`.
