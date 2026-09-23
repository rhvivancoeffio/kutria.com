import { defineStore } from 'pinia'
import { computed, ref } from 'vue'
import { resolveTenantSlug, tenantPathHint } from '@/utils/tenant'

/**
 * Commerce tenant context powered by Finbuckle route strategy (`/t/{identifier}`).
 * Replaces TemplateProject Account/Workspace as the multi-tenant boundary.
 */
export const useTenantStore = defineStore('tenant', () => {
  const identifier = ref(resolveTenantSlug())

  function refreshFromPath() {
    identifier.value = resolveTenantSlug()
  }

  const pathHint = computed(() => tenantPathHint(identifier.value))
  const displayName = computed(() => identifier.value)

  return {
    identifier,
    displayName,
    pathHint,
    hostHint: pathHint,
    refreshFromPath,
    refreshFromHost: refreshFromPath
  }
})
