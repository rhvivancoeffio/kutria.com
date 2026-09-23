---
name: create-frontend-feature
description: Create Vue 3 frontend features for Commerce.Web (cloned from TemplateProject.Frontend). Use when adding a view, admin page, component, slider modal, or API method. Tenancy is Finbuckle — not Account/Workspace. Slider headers close with an X icon, not a Cerrar text button. Slider footer actions use rounded-lg buttons — never btn-primary/btn-secondary pills (rounded-full).
---

# Create Frontend Feature (Commerce.Web)

Commerce.Web is the TemplateProject.Frontend SPA adapted for **Finbuckle.MultiTenant**.

> Before changing tenancy, read `.cursor/skills/finbuckle-tenancy/SKILL.md`.

## Quick Start

1. Add API methods in `src/services/api.js` (axios `api` already sends `X-Tenant`)
2. Create view in `src/views/{section}/`
3. Add route in `src/router/`
4. Use `apiService` + `useToast`

## Tenancy (do not regress)

```js
import { resolveTenantSlug } from '@/utils/tenant'
import { useTenantStore } from '@/stores/tenantStore'
```

- Tenant comes from the path `/t/{identifier}`, not a subdomain and not workspace localStorage
- Never add `X-Workspace-Id` as the isolation mechanism

## Project Structure

```
src/
├── views/
├── components/
├── layouts/          # AdminLayout shows Finbuckle tenant badge
├── router/
├── services/api.js
├── stores/
│   ├── appStore.js
│   └── tenantStore.js   # Finbuckle tenant
└── utils/tenant.js
```

## Slider modal

A right-side slider closes from the header with an X icon, never a text button labeled Cerrar. Keep `aria-label="Cerrar"`.

### Footer action buttons (required)

Do **not** use `btn-primary` / `btn-secondary` in slider footers. Those classes are `rounded-full` (marketing pills). Slider footers use **`rounded-lg`**, same as `CatalogCreateProductSlider`.

```html
<div class="shrink-0 px-4 sm:px-6 py-4 bg-gray-50 dark:bg-gray-700/50 border-t border-gray-200 dark:border-gray-700 flex flex-col-reverse sm:flex-row sm:justify-end gap-2">
  <button
    type="button"
    class="w-full sm:w-auto min-h-[44px] touch-manipulation px-4 py-2.5 sm:py-2 rounded-lg border border-gray-300 dark:border-gray-600 text-sm font-medium text-gray-700 dark:text-gray-200 hover:bg-gray-100 dark:hover:bg-gray-700 disabled:opacity-50"
    @click="close"
  >
    Cancelar
  </button>
  <button
    type="submit"
    class="w-full sm:w-auto min-h-[44px] touch-manipulation px-4 py-2.5 sm:py-2 rounded-lg bg-primary-600 hover:bg-primary-700 text-sm font-medium text-white disabled:opacity-50 disabled:cursor-not-allowed"
  >
    Guardar
  </button>
</div>
```

- Secondary: border + `rounded-lg`
- Primary: `bg-primary-600` + `rounded-lg`
- Never `rounded-full` / never bare `btn-primary` / `btn-secondary` in slider footers
- If using `.form-actions-footer` with `btn-*`, CSS forces `rounded-lg` — still prefer the explicit classes above

### Header close control

```html
<button
  type="button"
  class="p-2 -m-2 shrink-0 rounded-lg text-gray-500 hover:bg-gray-100 hover:text-gray-700 dark:hover:bg-gray-700 dark:hover:text-gray-300"
  aria-label="Cerrar"
  @click="close"
>
  <svg class="h-5 w-5" fill="none" stroke="currentColor" viewBox="0 0 24 24" aria-hidden="true">
    <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M6 18L18 6M6 6l12 12" />
  </svg>
</button>
```

## Checklist

- [ ] Route registered
- [ ] API via `apiService` (inherits `X-Tenant`)
- [ ] No new Account/Workspace tenancy assumptions
- [ ] Slider header closes with X icon (`aria-label="Cerrar"`)
- [ ] Slider footer buttons use `rounded-lg` (not `btn-primary` / `rounded-full`)
- [ ] Integrations list: Channels visual — `sm:grid-cols-2`, connected card action grid, available whole-card CTA (not 3-col / tinted text-link bars)
- [ ] English preferred for new code/comments (existing template UI may still be Spanish)
