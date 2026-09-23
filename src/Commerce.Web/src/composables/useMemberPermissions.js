import { computed, inject, unref } from 'vue'
import { useAppStore } from '../stores/appStore'
import { MEMBER_PERM } from '../constants/memberPermissions'
import { getPermissionsFromToken, hasPermission as hasPermissionUtil, isSuperAdmin } from '../utils/jwtUtils'

/**
 * Member permission helpers from JWT + profile fallback.
 * @param {{ profile?: { isAccountOwner?: boolean, permissions?: string[] } | null }} [options]
 */
export function useMemberPermissions(options = {}) {
  const injectedCan = inject('memberCan', null)
  const profile = computed(() => options.profile?.value ?? options.profile ?? null)

  const isAccountOwner = computed(
    () => profile.value?.isAccountOwner === true
  )

  const permissions = computed(() => {
    const fromProfile = profile.value?.permissions
    if (Array.isArray(fromProfile) && fromProfile.length > 0) return fromProfile
    return getPermissionsFromToken()
  })

  const can = (permissionKey) => {
    const explicitProfile = unref(options.profile)
    if (explicitProfile != null) {
      return hasPermissionUtil(permissionKey, {
        permissions: permissions.value,
        isAccountOwner: isAccountOwner.value || isSuperAdmin()
      })
    }
    if (injectedCan) return injectedCan(permissionKey)
    return hasPermissionUtil(permissionKey, {
      permissions: permissions.value,
      isAccountOwner: isAccountOwner.value || isSuperAdmin()
    })
  }

  return {
    isAccountOwner,
    permissions,
    can
  }
}

/**
 * Permisos de miembro usando el perfil cacheado en appStore (JWT + /auth/me).
 * Preferir esto en vistas admin en lugar de useResourcePermissions sin perfil.
 * @param {keyof typeof MEMBER_PERM} resourceKey
 */
export function useAppResourcePermissions(resourceKey) {
  const appStore = useAppStore()
  return useResourcePermissions(resourceKey, {
    profile: computed(() => appStore.profile)
  })
}

/**
 * @param {keyof typeof MEMBER_PERM} resourceKey
 * @param {{ profile?: import('vue').MaybeRef<{ isAccountOwner?: boolean, permissions?: string[] } | null> }} [options]
 */
export function useResourcePermissions(resourceKey, options = {}) {
  const { can } = useMemberPermissions(options)
  const keys = MEMBER_PERM[resourceKey] ?? {}

  const wrap = (action) =>
    computed(() => {
      const permKey = keys[action]
      return permKey ? can(permKey) : false
    })

  return {
    canList: wrap('list'),
    canCreate: wrap('create')
  }
}
