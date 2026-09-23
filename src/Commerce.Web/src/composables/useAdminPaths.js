import { computed } from 'vue'
import { useRoute } from 'vue-router'

/**
 * Paths under `/admin/*` (workspace context lives in header, not URL).
 * Kept as a composable so channel/data-pipeline views can build links consistently.
 */
export function useAdminPaths() {
  const route = useRoute()
  const isAdmin = computed(() => route.path.startsWith('/admin'))

  /** @param {string} pathWithinAdmin ej. 'data-pipeline/new' */
  function toPath(pathWithinAdmin) {
    const parts = String(pathWithinAdmin || '').split('/').filter(Boolean)
    const suffix = parts.length ? parts.join('/') : ''
    return suffix ? `/admin/${suffix}` : '/admin'
  }

  return {
    isAdmin,
    toPath,
    channels: computed(() => '/admin/channels/destinations'),
    integraciones: computed(() => '/admin/integraciones'),
    profile: computed(() => '/admin/profile'),
    changePassword: computed(() => '/admin/change-password')
  }
}
