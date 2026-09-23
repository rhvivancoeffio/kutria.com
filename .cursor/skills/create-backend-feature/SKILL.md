---
name: create-backend-feature
description: Create CQRS vertical-slice features for Commerce.* following MediatR, FluentValidation, Carter, and Finbuckle multi-tenancy. Use when adding a new feature, command, query, handler, validator, Carter module, or worker-invoked use case.
---

# Create Backend Feature (Commerce.*)

Creates CQRS features for **Commerce.Application** and **Commerce.Api** following Commerce conventions.

> Shared Database + `TenantId` column isolation via Finbuckle. Do **not** introduce Channels, Integrations, or DSL projects.
>
> **Low cost:** follow `.cursor/skills/low-cost/SKILL.md` — prefer Table Storage, event-stream `EventStreams:Transport` in appsettings (`Sse` default / `TableStorage` poll), batch writes, no new paid services unless required.

## Quick Start

1. Determine type: **Command** (write) or **Query** (read)
2. Create folder: `src/Commerce.Application/Features/{Feature}/{UseCase}/`
3. Add **one type per file**: Command/Query, Handler, Result, Validator (commands)
4. Add or extend Carter module in `src/Commerce.Api/Modules/{Feature}Module.cs`
5. Never reference sibling feature folders (deleting Publishing must not break Scheduling)

## Folder Structure

```
Features/{Feature}/{UseCase}/
├── {UseCase}Command.cs          # record only
├── {UseCase}Handler.cs
├── {UseCase}Result.cs           # record only
└── {UseCase}CommandValidator.cs # commands that modify data
```

Record files contain **only** the record definition.

## Command Template

```csharp
using Commerce.Application.Common.Abstracts;

namespace Commerce.Application.Features.{Feature}.{UseCase};

public sealed record {UseCase}Command(string RequiredParam) : ICommand<{UseCase}Result>;
```

```csharp
namespace Commerce.Application.Features.{Feature}.{UseCase};

public sealed record {UseCase}Result(Guid Id, string Name);
```

## Query Template

```csharp
using Commerce.Application.Common.Abstracts;

namespace Commerce.Application.Features.{Feature}.{UseCase};

public sealed record {UseCase}Query(Guid? FilterId = null) : IQuery<{UseCase}Result>;
```

## Handler Template (Command)

```csharp
using Finbuckle.MultiTenant.Abstractions;
using Commerce.Application.Abstracts;
using Commerce.Application.Common.Abstracts;
using Commerce.Domain.Tenants;

namespace Commerce.Application.Features.{Feature}.{UseCase};

public sealed class {UseCase}Handler(
    ICommerceDbContext db,
    IMultiTenantContextAccessor<CommerceTenantInfo> tenantAccessor)
    : ICommandHandler<{UseCase}Command, {UseCase}Result>
{
    public async Task<{UseCase}Result> Handle({UseCase}Command request, CancellationToken cancellationToken)
    {
        var tenant = tenantAccessor.MultiTenantContext?.TenantInfo
            ?? throw new InvalidOperationException("Tenant is required.");

        // Business logic — rely on HasQueryFilter / IsMultiTenant for reads
        await db.SaveChangesAsync(cancellationToken);
        return new {UseCase}Result(Guid.NewGuid(), tenant.Identifier);
    }
}
```

## Validator Template

```csharp
using FluentValidation;

namespace Commerce.Application.Features.{Feature}.{UseCase};

public sealed class {UseCase}CommandValidator : AbstractValidator<{UseCase}Command>
{
    public {UseCase}CommandValidator()
    {
        RuleFor(x => x.RequiredParam).NotEmpty().MaximumLength(200);
    }
}
```

## Carter Module Template

In `src/Commerce.Api/Modules/{Feature}Module.cs`:

```csharp
using Carter;
using MediatR;

namespace Commerce.Api.Modules;

public sealed class {Feature}Module : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        Map(app.MapGroup("/t/{__tenant__}/{resource}").WithTags("{Feature}"));
    }

    private static void Map(RouteGroupBuilder group)
    {
        group.MapPost("/", async (IMediator mediator, CancellationToken ct) =>
        {
            var result = await mediator.Send(new {UseCase}Command("value"), ct);
            return Results.Ok(result);
        });
    }
}
```

## Worker use case (optional)

If scheduled: add `PeriodicTimer` `BackgroundService` under `src/Commerce.Worker/Workers/`, create a scope, call `TenantBootstrap.SetCurrentTenant`, then `IMediator.Send`. No n8n.

## Hexagonal rules

- Ports in `Commerce.Application/Abstracts` (or feature-local `Contracts/`)
- Adapters only in `Commerce.Infrastructure`

## YAML metadata (host `data/`)

- **`IYamlMetadataService` / `YamlMetadataService`**: generic I/O only — resolve `data/` root, read files, deserialize YamlDotNet documents. **No** domain DTO mapping.
  - Default deserialize (`DeserializeRequiredFromYamlDocument`, `DeserializeAsync`, …): **CamelCase** (integrations and other host catalogs).
  - Agents only: `DeserializeAgentYamlDocument` — underscored naming + converters under `Commerce.Infrastructure/Agents` (`AgentYamlDocument`, `AgentYamlConverters`).
- **Feature services own parsing/mapping**: e.g. `IntegrationsMetadataService` reads `data/integrations/*.integration.yaml` via `IYamlMetadataService`, then maps YAML models → `IntegrationMetadataDto`. Do **not** put a second YamlDotNet `IDeserializer` in feature services.
- Do **not** bake OpenAPI/Postman schema blobs into integration YAML; catalog settings live in YAML, vendor specs stay out of Commerce.

## Tenancy rules

- Inject `ITenantInfo` / `CommerceTenantInfo` via `IMultiTenantContextAccessor<CommerceTenantInfo>`
- Every business entity implements `ITenantScoped` and uses Finbuckle `IsMultiTenant()`
- Shared Database + `TenantId` column — never database-per-tenant

## Checklist

- [ ] Namespace: `Commerce.Application.Features.{Feature}.{UseCase}`
- [ ] One file = one responsibility; records alone in their files
- [ ] Command/Query implements `ICommand<T>` / `IQuery<T>`
- [ ] Handler implements `ICommandHandler` / `IQueryHandler`
- [ ] Validator for write commands
- [ ] Carter module under `/t/{__tenant__}/...` only (no host twin)
- [ ] No cross-feature project references
- [ ] English only in code, comments, file names
- [ ] MediatR auto-registration (no manual handler DI)
