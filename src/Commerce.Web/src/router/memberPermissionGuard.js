import apiService from '../services/api'
import { ROUTE_MEMBER_PERMISSION } from '../constants/memberPermissions'
import { getPermissionsFromToken, hasPermission, isSuperAdmin } from '../utils/jwtUtils'

function resolveRequiredPermission(to) {
  if (to.meta?.memberPermission) return to.meta.memberPermission
  return ROUTE_MEMBER_PERMISSION[to.name] ?? null
}

function canAccessRoute(required, profile) {
  if (!required) return true
  if (isSuperAdmin()) return true
  if (profile?.isAccountOwner === true || profile?.IsAccountOwner === true) return true
  const perms =
    Array.isArray(profile?.permissions) && profile.permissions.length > 0
      ? profile.permissions
      : getPermissionsFromToken()
  return hasPermission(required, { permissions: perms, isAccountOwner: false })
}

/**
 * Blocks members without the route permission (owners and super admins pass).
 */
export async function memberPermissionBeforeEnter(to, from, next) {
  const required = resolveRequiredPermission(to)
  if (!required) {
    next()
    return
  }
  if (isSuperAdmin()) {
    next()
    return
  }
  const tokenPerms = getPermissionsFromToken()
  if (hasPermission(required, { permissions: tokenPerms, isAccountOwner: false })) {
    next()
    return
  }
  try {
    const profile = await apiService.getProfile()
    if (canAccessRoute(required, profile)) {
      next()
      return
    }
  } catch {
    /* fall through */
  }
  next({ name: 'AdminForbidden', replace: true })
}

function chainBeforeEnter(existing, guard) {
  if (!existing) return guard
  return async (to, from, next) => {
    await existing(to, from, (intermediate) => {
      if (intermediate === undefined || intermediate === true) {
        guard(to, from, next)
      } else {
        next(intermediate)
      }
    })
  }
}

/** Attach member permission guard to admin child routes. */
export function withMemberPermissionGuards(routes) {
  return routes.map((route) => {
    const nextRoute = { ...route }
    nextRoute.beforeEnter = chainBeforeEnter(route.beforeEnter, memberPermissionBeforeEnter)
    if (route.children?.length) {
      nextRoute.children = withMemberPermissionGuards(route.children)
    }
    return nextRoute
  })
}
