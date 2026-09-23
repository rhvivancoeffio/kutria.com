---
name: finbuckle-tenancy
description: Commerce multi-tenancy uses Finbuckle.MultiTenant path `/t/{identifier}`. Use when cloning from template-project, touching tenants, workspaces, accounts, or Commerce.Web API paths.
---

# Finbuckle.MultiTenant is the Commerce tenant engine

When cloning or adapting **template-project**, do **not** treat Account / Workspace as the tenant boundary.

| Template-project | Commerce |
|------------------|-----|
| `IAccountContext` / JWT `account_id` | Finbuckle `ITenantInfo` / `CommerceTenantInfo` |
| `X-Workspace-Id` + localStorage workspace | Path `/t/{identifier}` (header `X-Tenant` only as embed fallback) |
| Workspace switcher modal | Tenant from the URL path (see `useTenantStore`) |

## Backend (non-negotiable)

- Packages: `Finbuckle.MultiTenant` + `Finbuckle.MultiTenant.EntityFrameworkCore`
- Strategy: Shared Database + `TenantId` column + `IsMultiTenant()` / query filters
- Resolve by route `/t/{__tenant__}/...`. Do not use a tenant subdomain. `X-Tenant` is only a fallback for the embed script.
- Seed: `tenant1`, `tenant2`, `tenant3`

## Frontend (Commerce.Web)

- Resolve slug: `src/utils/tenant.js` → `resolveTenantSlug()`
- Pinia: `src/stores/tenantStore.js`
- Axios prefixes tenant calls with `/t/{identifier}` (`withTenantApiPath`)
- Static Web App hosts the SPA. The API is a separate Web App. Do not require `*.localhost` or `*.kutria.com`.

## Checklist when copying from template

- [ ] Do **not** reintroduce `X-Workspace-Id` as the tenancy header
- [ ] Do **not** require workspace selection before API calls
- [ ] Keep Finbuckle path UX: `http://localhost:3000/t/tenant1`
- [ ] Keep backend `HasQueryFilter` / `IsMultiTenant` intact
